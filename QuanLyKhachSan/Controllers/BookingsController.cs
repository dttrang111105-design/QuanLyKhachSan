
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels;

public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public BookingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Customer"))
        {
            return RedirectToAction(nameof(MyBookings));
        }

        var bookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(x => x.Room)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    // GET: BOOKINGS/Details/5
    [Authorize]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Room)
            .Include(b => b.ServiceBookings)
                .ThenInclude(sb => sb.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound();

        if (User.IsInRole("Customer"))
        {
            var customer = await GetCurrentCustomer();

            if (customer == null || booking.CustomerId != customer.Id)
            {
                return Forbid();
            }
        }

        return View(booking);
    }

    // GET: BOOKINGS/Create
    [Authorize(Roles = "Customer")]
    public IActionResult Create(int roomId)
    {
        var room = _context.Rooms.FirstOrDefault(x => x.Id == roomId);

        if (room == null)
            return NotFound();

        if (room.Status == RoomStatus.Maintenance)
        {
            TempData["Error"] = "Phòng đang bảo trì.";

            return RedirectToAction("Index", "Rooms");
        }

        BookingCreateViewModel vm = new()
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            PricePerNight = room.PriceDay,
            CheckInDate = DateTime.Today,
            CheckOutDate = DateTime.Today.AddDays(1),
            Adult = 1
        };

        return View(vm);
    }

    // POST: BOOKINGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var customer = await _context.Customers
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Account!.Username == User.Identity!.Name);

        if (customer == null)
            return Unauthorized();

        var room = await _context.Rooms.FindAsync(vm.RoomId);

        if (room == null)
        {
            ModelState.AddModelError("", "Không tìm thấy phòng.");
            return View(vm);
        }

        // Chỉ cấm phòng đang bảo trì
        if (room.Status == RoomStatus.Maintenance)
        {
            ModelState.AddModelError("", "Phòng đang bảo trì.");
            return View(vm);
        }

        if (vm.CheckInDate >= vm.CheckOutDate)
        {
            ModelState.AddModelError("", "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
            return View(vm);
        }

        if (vm.Adult + vm.Children > room.MaxOccupancy)
        {
            ModelState.AddModelError("", "Số lượng khách vượt quá sức chứa của phòng.");
            return View(vm);
        }

        // Kiểm tra trùng lịch
        bool booked = await _context.BookingDetails
            .Include(x => x.Booking)
            .AnyAsync(x =>
                x.RoomId == vm.RoomId &&
                vm.CheckInDate < x.Booking!.CheckOutDate &&
                vm.CheckOutDate > x.Booking.CheckInDate &&
                x.Booking.Status != BookingStatus.Cancelled &&
                x.Booking.Status != BookingStatus.CheckedOut);

        if (booked)
        {
            ModelState.AddModelError("", "Phòng đã được đặt trong khoảng thời gian này.");
            return View(vm);
        }

        int nights = (vm.CheckOutDate - vm.CheckInDate).Days;

        Booking booking = new Booking
        {
            CustomerId = customer.Id,
            BookingCode = "BK" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            BookingDate = DateTime.Now,
            CheckInDate = vm.CheckInDate,
            CheckOutDate = vm.CheckOutDate,
            Adult = vm.Adult,
            Children = vm.Children,
            Deposit = vm.Deposit,
            Note = vm.Note,
            Status = BookingStatus.Pending,
            TotalAmount = room.PriceDay * nights
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            BookingDetail detail = new BookingDetail
            {
                BookingId = booking.Id,
                RoomId = room.Id,
                PricePerNight = room.PriceDay,
                NumberOfNights = nights,
                DiscountPercent = 0,
                TotalPrice = booking.TotalAmount
            };

            _context.BookingDetails.Add(detail);

            // KHÔNG đổi trạng thái phòng ở đây

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["Success"] = "Đặt phòng thành công.";

            return RedirectToAction(nameof(MyBookings));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);

            return View(vm);
        }
    }


    [Authorize(Roles = "Admin,Receptionist")]
    // GET: BOOKINGS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        var booking = await _context.Bookings.FindAsync(id);

        if (booking == null)
            return NotFound();

        if (User.IsInRole("Customer"))
        {
            var customer = await GetCurrentCustomer();

            if (customer == null || booking.CustomerId != customer.Id)
                return Forbid();
        }
        return View(booking);
    }

    // POST: BOOKINGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int id,
    Booking booking)
    {
        if (id != booking.Id)
            return NotFound();

        var oldBooking = await _context.Bookings
            .Include(x => x.BookingDetails)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (oldBooking == null)
            return NotFound();

        if (booking.CheckInDate >= booking.CheckOutDate)
        {
            ModelState.AddModelError("", "Ngày trả phòng phải lớn hơn ngày nhận.");
            return View(booking);
        }

        // Kiểm tra trùng lịch cho từng phòng
        foreach (var detail in oldBooking.BookingDetails)
        {
            bool overlap = await _context.BookingDetails
                .Include(x => x.Booking)
                .AnyAsync(x =>
                    x.RoomId == detail.RoomId &&
                    x.BookingId != booking.Id &&
                    booking.CheckInDate < x.Booking!.CheckOutDate &&
                    booking.CheckOutDate > x.Booking.CheckInDate &&
                    x.Booking.Status != BookingStatus.Cancelled &&
                    x.Booking.Status != BookingStatus.CheckedOut);

            if (overlap)
            {
                ModelState.AddModelError("", $"Phòng {detail.RoomId} đã được đặt trong khoảng thời gian này.");
                return View(booking);
            }
        }

        oldBooking.CheckInDate = booking.CheckInDate;
        oldBooking.CheckOutDate = booking.CheckOutDate;
        oldBooking.Adult = booking.Adult;
        oldBooking.Children = booking.Children;
        oldBooking.Deposit = booking.Deposit;
        oldBooking.Note = booking.Note;
        oldBooking.Status = booking.Status;

        int nights = (booking.CheckOutDate - booking.CheckInDate).Days;

        decimal total = 0;

        foreach (var detail in oldBooking.BookingDetails)
        {
            detail.NumberOfNights = nights;
            detail.TotalPrice = detail.PricePerNight * nights;
            total += detail.TotalPrice;

            var room = await _context.Rooms.FindAsync(detail.RoomId);

            if (room != null)
            {
                switch (booking.Status)
                {
                    case BookingStatus.Pending:
                    case BookingStatus.Confirmed:
                    case BookingStatus.Cancelled:
                    case BookingStatus.CheckedOut:
                        room.Status = RoomStatus.Available;
                        room.IsAvailable = true;
                        break;

                    case BookingStatus.CheckedIn:
                        room.Status = RoomStatus.Occupied;
                        room.IsAvailable = false;
                        break;
                }
            }
        }

        oldBooking.TotalAmount = total;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật Booking thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: BOOKINGS/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(x => x.Id == id);

        if (booking == null)
            return NotFound();

        if (User.IsInRole("Customer"))
        {
            var customer = await GetCurrentCustomer();

            if (customer == null || booking.CustomerId != customer.Id)
                return Forbid();
        }

        return View(booking);
    }

    //khi xóa booking -> phòng không bị reserved
    // POST: BOOKINGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var booking = await _context.Bookings.FindAsync(id);

        if (booking == null)
            return NotFound();

        var details = await _context.BookingDetails
            .Where(x => x.BookingId == booking.Id)
            .ToListAsync();

        _context.BookingDetails.RemoveRange(details);

        _context.Bookings.Remove(booking);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa Booking thành công.";

        return RedirectToAction(nameof(Index));
    }

    private bool BookingExists(int? id)
    {
        return _context.Bookings.Any(e => e.Id == id);
    }

    //khách chỉ xem đc booking của mình
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyBookings()
    {
        var customer = await GetCurrentCustomer();

        if (customer == null)
            return NotFound();

        var bookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Room)
            .Where(b => b.CustomerId == customer.Id)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View("Index", bookings);
    }

    private async Task<Customer?> GetCurrentCustomer()
    {
        var username = User.Identity?.Name;

        return await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Account!.Username == username);
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings
            .Include(x => x.BookingDetails)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (booking == null)
            return NotFound();

        var customer = await GetCurrentCustomer();

        if (customer == null || booking.CustomerId != customer.Id)
            return Forbid();

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể hủy booking đang chờ.";

            return RedirectToAction(nameof(MyBookings));
        }

        booking.Status = BookingStatus.Cancelled;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Hủy booking thành công.";

        return RedirectToAction(nameof(MyBookings));
    }

    //check in
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var booking = await _context.Bookings
            .Include(x => x.BookingDetails)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (booking == null)
            return NotFound();

        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Confirmed)
        {
            TempData["Error"] = "Booking này không thể Check In.";

            return RedirectToAction(nameof(Index));
        }

        booking.Status = BookingStatus.CheckedIn;

        foreach (var item in booking.BookingDetails)
        {
            var room = await _context.Rooms.FindAsync(item.RoomId);

            if (room != null)
            {
                room.Status = RoomStatus.Occupied;
                room.IsAvailable = false;
            }
        }

        _context.Bookings.Update(booking);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Check In thành công.";

        return RedirectToAction(nameof(Index));
    }

    //check out
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Room)
            .Include(b => b.ServiceBookings)
            .Include(b => b.Invoice)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return NotFound();

        if (booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = "Chỉ có thể trả phòng khi khách đang lưu trú.";
            return RedirectToAction(nameof(Index));
        }

        // Tính tiền phòng
        decimal roomAmount = booking.BookingDetails.Sum(x => x.TotalPrice);

        // Tính tiền dịch vụ
        decimal serviceAmount = booking.ServiceBookings.Sum(x => x.TotalPrice);

        // Tổng trước thuế
        decimal subTotal = roomAmount + serviceAmount;

        // Thuế VAT 8%
        decimal taxPercent = 8;
        decimal taxAmount = subTotal * taxPercent / 100;

        // Tổng sau thuế
        decimal total = subTotal + taxAmount;

        if (booking.Invoice == null)
        {
            var invoice = new Invoice
            {
                BookingId = booking.Id,
                InvoiceCode = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                InvoiceDate = DateTime.Now,

                RoomAmount = roomAmount,
                ServiceAmount = serviceAmount,

                DiscountPercent = 0,
                TaxPercent = taxPercent,

                TotalAmount = total
            };

            _context.Invoices.Add(invoice);
        }
        else
        {
            booking.Invoice.RoomAmount = roomAmount;
            booking.Invoice.ServiceAmount = serviceAmount;
            booking.Invoice.DiscountPercent = 0;
            booking.Invoice.TaxPercent = taxPercent;
            booking.Invoice.TotalAmount = total;
        }

        booking.Status = BookingStatus.CheckedOut;

        foreach (var detail in booking.BookingDetails)
        {
            detail.Room.Status = RoomStatus.Available;
            detail.Room.IsAvailable = true;
        }

        await _context.SaveChangesAsync();

        var invoiceId = booking.Invoice?.Id;

        if (invoiceId == null)
        {
            invoiceId = await _context.Invoices
                .Where(x => x.BookingId == booking.Id)
                .Select(x => x.Id)
                .FirstAsync();
        }

        return RedirectToAction("Details", "Invoices", new { id = invoiceId });
    }
}
