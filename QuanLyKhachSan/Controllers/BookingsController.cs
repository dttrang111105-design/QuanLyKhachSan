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
    [HttpGet]
    public async Task<IActionResult> Create(
    int roomId,
    DateTime? checkInDate,
    DateTime? checkOutDate,
    int adult = 1,
    int children = 0)
    {
        // Tìm phòng theo id.
        var room = await _context.Rooms
            .FirstOrDefaultAsync(x =>
                x.Id == roomId &&
                !x.IsDeleted
            );

        if (room == null)
        {
            return NotFound();
        }

        // Không cho đặt phòng đang bảo trì.
        if (room.Status == RoomStatus.Maintenance)
        {
            TempData["Error"] = "Phòng đang bảo trì.";

            return RedirectToAction(
                "Search",
                "Rooms"
            );
        }

        // Lấy ngày nhận được truyền từ trang tìm kiếm.
        var selectedCheckIn =
            (checkInDate ?? DateTime.Today).Date;

        // Lấy ngày trả được truyền từ trang tìm kiếm.
        var selectedCheckOut =
            (checkOutDate ?? selectedCheckIn.AddDays(1)).Date;

        // Nếu ngày nhận nhỏ hơn hôm nay thì đặt lại thành hôm nay.
        if (selectedCheckIn < DateTime.Today)
        {
            selectedCheckIn = DateTime.Today;
        }

        // Nếu ngày trả không hợp lệ thì tự tăng thêm một ngày.
        if (selectedCheckOut <= selectedCheckIn)
        {
            selectedCheckOut = selectedCheckIn.AddDays(1);
        }

        var viewModel = new BookingCreateViewModel
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            PricePerNight = room.PriceDay,
            CheckInDate = selectedCheckIn,
            CheckOutDate = selectedCheckOut,
            Adult = Math.Max(1, adult),
            Children = Math.Max(0, children)
        };

        return View(viewModel);
    }

    // POST: BOOKINGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    BookingCreateViewModel vm)
    {
        // Tìm lại phòng từ database.
        var room = await _context.Rooms
            .FirstOrDefaultAsync(x =>
                x.Id == vm.RoomId &&
                !x.IsDeleted
            );

        if (room == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không tìm thấy phòng."
            );

            return View(vm);
        }

        /*
         * Các trường RoomNumber và PricePerNight không cho khách sửa.
         * Vì vậy phải nạp lại từ database.
         */
        vm.RoomNumber = room.RoomNumber;
        vm.PricePerNight = room.PriceDay;

        vm.CheckInDate = vm.CheckInDate.Date;
        vm.CheckOutDate = vm.CheckOutDate.Date;

        // Kiểm tra ngày nhận.
        if (vm.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(vm.CheckInDate),
                "Ngày nhận phòng không được nhỏ hơn ngày hiện tại."
            );
        }

        // Kiểm tra ngày trả.
        if (vm.CheckInDate >= vm.CheckOutDate)
        {
            ModelState.AddModelError(
                nameof(vm.CheckOutDate),
                "Ngày trả phòng phải lớn hơn ngày nhận phòng."
            );
        }

        // Kiểm tra phòng bảo trì.
        if (room.Status == RoomStatus.Maintenance)
        {
            ModelState.AddModelError(
                string.Empty,
                "Phòng đang bảo trì."
            );
        }

        // Kiểm tra sức chứa.
        if (vm.Adult + vm.Children > room.MaxOccupancy)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Phòng chỉ chứa tối đa {room.MaxOccupancy} người."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Tìm khách hàng đang đăng nhập.
        var customer = await _context.Customers
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x =>
                x.Account != null &&
                x.Account.Username == User.Identity!.Name &&
                !x.IsDeleted
            );

        if (customer == null)
        {
            return Unauthorized();
        }

        /*
         * Kiểm tra lại phòng đã bị người khác đặt chưa.
         *
         * Việc kiểm tra lại rất quan trọng vì từ lúc khách tìm phòng
         * đến lúc khách bấm đặt phòng, một người khác có thể đã đặt.
         */
        bool booked = await _context.BookingDetails
            .Include(x => x.Booking)
            .AnyAsync(x =>
                x.RoomId == vm.RoomId &&
                !x.IsDeleted &&
                x.Booking != null &&
                !x.Booking.IsDeleted &&
                vm.CheckInDate < x.Booking.CheckOutDate &&
                vm.CheckOutDate > x.Booking.CheckInDate &&
                x.Booking.Status != BookingStatus.Cancelled &&
                x.Booking.Status != BookingStatus.CheckedOut
            );

        if (booked)
        {
            ModelState.AddModelError(
                string.Empty,
                "Phòng đã được đặt trong khoảng thời gian này."
            );

            return View(vm);
        }

        // Tính số đêm.
        int numberOfNights =
            (vm.CheckOutDate - vm.CheckInDate).Days;

        // Tính tổng tiền phòng.
        decimal totalAmount =
            room.PriceDay * numberOfNights;

        // Tạo Booking.
        var booking = new Booking
        {
            CustomerId = customer.Id,

            BookingCode =
                "BK" + DateTime.Now.ToString("yyyyMMddHHmmss"),

            BookingDate = DateTime.Now,

            CheckInDate = vm.CheckInDate,

            CheckOutDate = vm.CheckOutDate,

            Adult = vm.Adult,

            Children = vm.Children,

            Deposit = vm.Deposit,

            Note = vm.Note?.Trim(),

            Status = BookingStatus.Pending,

            TotalAmount = totalAmount,

            CreatedAt = DateTime.Now,

            IsDeleted = false
        };

        // Dùng transaction để Booking và BookingDetail được lưu cùng nhau.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Lưu Booking trước để lấy BookingId.
            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            // Tạo chi tiết Booking.
            var bookingDetail = new BookingDetail
            {
                BookingId = booking.Id,

                RoomId = room.Id,

                PricePerNight = room.PriceDay,

                NumberOfNights = numberOfNights,

                DiscountPercent = 0,

                TotalPrice = totalAmount,

                CreatedAt = DateTime.Now,

                IsDeleted = false
            };

            _context.BookingDetails.Add(bookingDetail);

            /*
             * Không đổi trạng thái phòng sang Reserved tại đây.
             *
             * Khả năng phòng trống được xác định bằng khoảng ngày Booking,
             * không nên chỉ phụ thuộc vào Room.Status.
             */

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["Success"] =
                "Đặt phòng thành công.";

            return RedirectToAction(
                nameof(MyBookings)
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                ex.InnerException?.Message ?? ex.Message
            );

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
            .Where(b =>
                b.CustomerId == customer.Id &&
                !b.IsDeleted)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        var roomIds = bookings
            .SelectMany(booking => booking.BookingDetails)
            .Where(detail => !detail.IsDeleted)
            .Select(detail => detail.RoomId)
            .Distinct()
            .ToList();

        var reviewByRoom = await _context.Reviews
            .AsNoTracking()
            .Where(review =>
                review.CustomerId == customer.Id &&
                roomIds.Contains(review.RoomId) &&
                !review.IsDeleted)
            .ToDictionaryAsync(
                review => review.RoomId,
                review => review.Id);

        ViewBag.ReviewByRoom = reviewByRoom;

        return View("Index", bookings);
    }

    private async Task<Customer?> GetCurrentCustomer()
    {
        string? username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }
        return await _context.Customers
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => !c.IsDeleted &&
                                      c.Account != null &&
                                      c.Account.Username == username);
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

    //Hàm tải danh sách khách hàng
    private async Task LoadStaffBookingFormAsync(BookingCreateViewModel model)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == model.RoomId && !r.IsDeleted);

        if (room != null)
        {
            model.RoomNumber = room.RoomNumber;
            model.RoomTypeName = room.RoomType?.Name ?? "Chưa xác định";
            model.PricePerNight = room.PriceDay;
            model.MaxOccupancy = room.MaxOccupancy;
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.FullName)
            .Select(c => new
            {
                c.Id,
                DisplayName = c.FullName + " - " + (string.IsNullOrWhiteSpace(c.Phone) ? "Chưa có SĐT" : c.Phone)
            })
            .ToListAsync();

        ViewBag.Customers = new SelectList(customers, "Id", "DisplayName", model.CustomerId);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> CreateForCustomer(int roomId)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        if (room.Status == RoomStatus.Maintenance)
        {
            TempData["Error"] = "Phòng đang bảo trì, không thể đặt.";
            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }

        var model = new BookingCreateViewModel
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            RoomTypeName = room.RoomType?.Name ?? "Chưa xác định",
            PricePerNight = room.PriceDay,
            MaxOccupancy = room.MaxOccupancy,
            CheckInDate = DateTime.Today,
            CheckOutDate = DateTime.Today.AddDays(1),
            Adult = 1,
            Children = 0,
            Deposit = 0
        };

        await LoadStaffBookingFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> CreateForCustomer(BookingCreateViewModel model)
    {
        var room = await _context.Rooms.Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == model.RoomId && !r.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        model.RoomNumber = room.RoomNumber;
        model.RoomTypeName = room.RoomType?.Name ?? "Chưa xác định";
        model.PricePerNight = room.PriceDay;
        model.MaxOccupancy = room.MaxOccupancy;
        model.CheckInDate = model.CheckInDate.Date;
        model.CheckOutDate = model.CheckOutDate.Date;
        model.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();

        Customer? selectedCustomer = null;

        if (model.CreateNewCustomer)
        {
            model.GuestFullName = model.GuestFullName?.Trim();
            model.GuestPhone = model.GuestPhone?.Trim();
            model.GuestEmail = string.IsNullOrWhiteSpace(model.GuestEmail) ? null : model.GuestEmail.Trim();
            model.GuestCitizenId = string.IsNullOrWhiteSpace(model.GuestCitizenId) ? null : model.GuestCitizenId.Trim();
            model.GuestAddress = string.IsNullOrWhiteSpace(model.GuestAddress) ? null : model.GuestAddress.Trim();
            model.GuestGender = string.IsNullOrWhiteSpace(model.GuestGender) ? null : model.GuestGender.Trim();

            if (string.IsNullOrWhiteSpace(model.GuestFullName))
            {
                ModelState.AddModelError(nameof(model.GuestFullName), "Vui lòng nhập họ tên khách hàng.");
            }

            if (string.IsNullOrWhiteSpace(model.GuestPhone))
            {
                ModelState.AddModelError(nameof(model.GuestPhone), "Vui lòng nhập số điện thoại.");
            }
        }
        else
        {
            if (!model.CustomerId.HasValue || model.CustomerId.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Vui lòng chọn khách hàng.");
            }
            else
            {
                selectedCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == model.CustomerId.Value && !c.IsDeleted);

                if (selectedCustomer == null)
                {
                    ModelState.AddModelError(nameof(model.CustomerId), "Khách hàng không tồn tại.");
                }
            }
        }

        if (model.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.CheckInDate), "Ngày nhận phòng không được nhỏ hơn ngày hiện tại.");
        }

        if (model.CheckOutDate <= model.CheckInDate)
        {
            ModelState.AddModelError(nameof(model.CheckOutDate), "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
        }

        if (room.Status == RoomStatus.Maintenance)
        {
            ModelState.AddModelError(string.Empty, "Phòng đang bảo trì.");
        }

        if (model.Adult + model.Children <= 0)
        {
            ModelState.AddModelError(string.Empty, "Booking phải có ít nhất một khách.");
        }

        if (model.Adult + model.Children > room.MaxOccupancy)
        {
            ModelState.AddModelError(string.Empty, $"Phòng chỉ chứa tối đa {room.MaxOccupancy} người.");
        }

        int numberOfNights = Math.Max(0, (model.CheckOutDate - model.CheckInDate).Days);
        decimal totalAmount = room.PriceDay * numberOfNights;

        if (model.Deposit > totalAmount)
        {
            ModelState.AddModelError(nameof(model.Deposit), "Tiền cọc không được lớn hơn tổng tiền phòng.");
        }

        bool isBooked = await _context.BookingDetails.AsNoTracking()
            .Include(d => d.Booking)
            .AnyAsync(d => d.RoomId == model.RoomId &&
                           !d.IsDeleted &&
                           d.Booking != null &&
                           !d.Booking.IsDeleted &&
                           model.CheckInDate < d.Booking.CheckOutDate &&
                           model.CheckOutDate > d.Booking.CheckInDate &&
                           d.Booking.Status != BookingStatus.Cancelled &&
                           d.Booking.Status != BookingStatus.CheckedOut);

        if (isBooked)
        {
            ModelState.AddModelError(string.Empty, "Phòng đã được đặt trong khoảng thời gian này.");
        }

        if (!ModelState.IsValid)
        {
            await LoadStaffBookingFormAsync(model);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (model.CreateNewCustomer)
            {
                selectedCustomer = new Customer
                {
                    AccountId = null,
                    FullName = model.GuestFullName!,
                    Gender = model.GuestGender,
                    DateOfBirth = model.GuestDateOfBirth,
                    Phone = model.GuestPhone,
                    Email = model.GuestEmail,
                    Address = model.GuestAddress,
                    CitizenId = model.GuestCitizenId,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };

                _context.Customers.Add(selectedCustomer);
                await _context.SaveChangesAsync();
            }

            var booking = new Booking
            {
                CustomerId = selectedCustomer!.Id,
                BookingCode = "BK" + DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                BookingDate = DateTime.Now,
                CheckInDate = model.CheckInDate,
                CheckOutDate = model.CheckOutDate,
                Adult = model.Adult,
                Children = model.Children,
                Deposit = model.Deposit,
                Note = model.Note,
                TotalAmount = totalAmount,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var bookingDetail = new BookingDetail
            {
                BookingId = booking.Id,
                RoomId = room.Id,
                PricePerNight = room.PriceDay,
                NumberOfNights = numberOfNights,
                DiscountPercent = 0,
                TotalPrice = totalAmount,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.BookingDetails.Add(bookingDetail);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đặt phòng {room.RoomNumber} cho khách {selectedCustomer.FullName} thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
            await LoadStaffBookingFormAsync(model);
            return View(model);
        }
    }

}