using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;
using QuanLyKhachSan.ViewModels;
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly ReceptionistActivityService _activityService;

    public BookingsController(
        ApplicationDbContext context,
        EmailService emailService,
        ReceptionistActivityService activityService)
    {
        _context = context;
        _emailService = emailService;
        _activityService = activityService;
    }
    [Authorize]
    public async Task<IActionResult> Index(string? filter)
    {
        if (User.IsInRole("Customer"))
        {
            return RedirectToAction(nameof(MyBookings));
        }
        DateTime today = DateTime.Today;
        DateTime tomorrow = today.AddDays(1);
        string selectedFilter = filter?.Trim() ?? string.Empty;
        var query = _context.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Room)
            .Where(b => !b.IsDeleted)
            .AsQueryable();
        switch (selectedFilter.ToLowerInvariant())
        {
            case "pending":
                query = query.Where(b => b.Status == BookingStatus.Pending);
                selectedFilter = "pending";
                break;
            case "checkintoday":
                query = query.Where(b =>
                    b.CheckInDate >= today &&
                    b.CheckInDate < tomorrow &&
                    b.Status != BookingStatus.Cancelled);
                selectedFilter = "checkinToday";
                break;
            case "checkouttoday":
                query = query.Where(b =>
                    b.CheckOutDate >= today &&
                    b.CheckOutDate < tomorrow &&
                    b.Status != BookingStatus.Cancelled);
                selectedFilter = "checkoutToday";
                break;
            default:
                selectedFilter = string.Empty;
                break;
        }
        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
        ViewBag.SelectedFilter = selectedFilter;
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
    int roomId, DateTime? checkInDate, DateTime? checkOutDate,
    int adult = 1, int children = 0)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(x => x.Id == roomId && !x.IsDeleted);
        if (room == null)
        {
            return NotFound();
        }
        if (room.Status == RoomStatus.Maintenance)
        {
            TempData["Error"] = "Phòng đang bảo trì.";
            return RedirectToAction("Search", "Rooms");
        }
        var selectedCheckIn = (checkInDate ?? DateTime.Today).Date;
        var selectedCheckOut = (checkOutDate ?? selectedCheckIn.AddDays(1)).Date;
        if (selectedCheckIn < DateTime.Today)
        {
            selectedCheckIn = DateTime.Today;
        }
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
    public async Task<IActionResult> Create(BookingCreateViewModel vm)
    {
        // Tìm lại phòng từ database.
        var room = await _context.Rooms
            .FirstOrDefaultAsync(x => x.Id == vm.RoomId && !x.IsDeleted);
        if (room == null)
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy phòng.");
            return View(vm);
        }

        vm.RoomNumber = room.RoomNumber;
        vm.PricePerNight = room.PriceDay;
        vm.CheckInDate = vm.CheckInDate.Date;
        vm.CheckOutDate = vm.CheckOutDate.Date;

        if (vm.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError(nameof(vm.CheckInDate), "Ngày nhận phòng không được nhỏ hơn ngày hiện tại.");
        }

        if (vm.CheckInDate >= vm.CheckOutDate)
        {
            ModelState.AddModelError(nameof(vm.CheckOutDate), "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
        }

        if (room.Status == RoomStatus.Maintenance)
        {
            ModelState.AddModelError(string.Empty, "Phòng đang bảo trì.");
        }
        if (vm.Adult + vm.Children > room.MaxOccupancy)
        {
            ModelState.AddModelError(string.Empty, $"Phòng chỉ chứa tối đa {room.MaxOccupancy} người.");
        }
        if (!ModelState.IsValid)
        {
            return View(vm);
        }
        // Tìm khách hàng đang đăng nhập.
        var customer = await _context.Customers
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Account != null &&
                x.Account.Username == User.Identity!.Name && !x.IsDeleted);
        if (customer == null)
        {
            return Unauthorized();
        }
        // Kiểm tra lại phòng đã bị người khác đặt chưa. 
        bool booked = await _context.BookingDetails
            .Include(x => x.Booking)
            .AnyAsync(x => x.RoomId == vm.RoomId &&
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
            ModelState.AddModelError(string.Empty, "Phòng đã được đặt trong khoảng thời gian này.");
            return View(vm);
        }

        int numberOfNights = (vm.CheckOutDate - vm.CheckInDate).Days;

        decimal totalAmount = room.PriceDay * numberOfNights;

        var booking = new Booking
        {
            CustomerId = customer.Id,
            BookingCode = "BK" + DateTime.Now.ToString("yyyyMMddHHmmss"),
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
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Lưu Booking trước để lấy BookingId.
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

            string safeCustomerName = System.Net.WebUtility.HtmlEncode(customer.FullName);
            string safeBookingCode = System.Net.WebUtility.HtmlEncode(booking.BookingCode);
            string safeRoomNumber = System.Net.WebUtility.HtmlEncode(room.RoomNumber);

            string emailBody = $@"
                <div style='font-family:Arial,sans-serif;line-height:1.6;color:#222'>
                    <h2 style='margin-bottom:12px'>Xác nhận đặt phòng thành công</h2>

                    <p>Xin chào <strong>{safeCustomerName}</strong>,</p>

                    <p>Luxury Hotel đã ghi nhận yêu cầu đặt phòng của bạn.</p>

                    <table style='border-collapse:collapse;margin:16px 0'>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Mã Booking:</strong></td>
                            <td style='padding:6px 0'>{safeBookingCode}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Phòng:</strong></td>
                            <td style='padding:6px 0'>{safeRoomNumber}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Ngày nhận phòng:</strong></td>
                            <td style='padding:6px 0'>{booking.CheckInDate:dd/MM/yyyy}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Ngày trả phòng:</strong></td>
                            <td style='padding:6px 0'>{booking.CheckOutDate:dd/MM/yyyy}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Số đêm:</strong></td>
                            <td style='padding:6px 0'>{numberOfNights}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Người lớn:</strong></td>
                            <td style='padding:6px 0'>{booking.Adult}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Trẻ em:</strong></td>
                            <td style='padding:6px 0'>{booking.Children}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Tiền phòng:</strong></td>
                            <td style='padding:6px 0'>{booking.TotalAmount:N0} VNĐ</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Tiền đặt cọc:</strong></td>
                            <td style='padding:6px 0'>{booking.Deposit:N0} VNĐ</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 18px 6px 0'><strong>Trạng thái:</strong></td>
                            <td style='padding:6px 0'>Đang chờ xác nhận</td>
                        </tr>
                    </table>

                    <p>Vui lòng lưu lại mã Booking để thuận tiện khi làm thủ tục nhận phòng.</p>

                    <p style='margin-top:24px'>
                        Cảm ơn bạn đã lựa chọn Luxury Hotel.
                    </p>
                </div>";

            bool emailSent = await _emailService.SendAsync(
                customer.Email,
                $"Xác nhận đặt phòng - {booking.BookingCode}",
                emailBody);

            TempData["Success"] = emailSent
                ? "Đặt phòng thành công. Email xác nhận đã được gửi."
                : "Đặt phòng thành công nhưng chưa thể gửi email xác nhận.";

            return RedirectToAction(nameof(MyBookings));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, ex.InnerException?.Message ?? ex.Message);
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
    public async Task<IActionResult> Edit(int id, Booking booking)
    {
        if (id != booking.Id)
            return NotFound();
        var oldBooking = await _context.Bookings
            .Include(x => x.BookingDetails)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (oldBooking == null)
            return NotFound();

        var previousStatus = oldBooking.Status;

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

        if (previousStatus != BookingStatus.Cancelled &&
            oldBooking.Status == BookingStatus.Cancelled)
        {
            await _activityService.AddForCurrentUserAsync(
                User,
                "CancelBooking",
                oldBooking,
                oldBooking.Invoice,
                $"Hủy Booking {oldBooking.BookingCode}");
        }

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
            .Where(b => b.CustomerId == customer.Id && !b.IsDeleted)
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
            .Where(review => review.CustomerId == customer.Id &&
                roomIds.Contains(review.RoomId) && !review.IsDeleted)
            .ToDictionaryAsync(review => review.RoomId, review => review.Id);
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
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.Account != null && c.Account.Username == username);
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

        await _activityService.AddForCurrentUserAsync(
            User,
            "CheckIn",
            booking,
            booking.Invoice,
            $"Check In Booking {booking.BookingCode}");

        await _context.SaveChangesAsync();
        TempData["Success"] = "Check In thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Room)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        if (booking == null)
            return NotFound();
        if (booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = "Chỉ có thể trả phòng khi khách đang lưu trú.";
            return RedirectToAction(nameof(Index));
        }
        bool inspected = await _context.RoomInspections
            .AnyAsync(x => x.BookingId == booking.Id && !x.IsDeleted);
        if (inspected)
        {
            TempData["Error"] = "Booking này đã được kiểm tra phòng.";
            return RedirectToAction(nameof(Index));
        }
        var model = await CreateRoomInspectionViewModelAsync(booking);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> ConfirmCheckOut(RoomInspectionViewModel model)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Room)
            .Include(b => b.ServiceBookings.Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Service)
            .Include(b => b.Invoice)
                .ThenInclude(i => i.InvoiceDetails)
            .FirstOrDefaultAsync(b => b.Id == model.BookingId && !b.IsDeleted);
        if (booking == null)
            return NotFound();
        if (booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = "Booking này không còn ở trạng thái đang lưu trú.";
            return RedirectToAction(nameof(Index));
        }
        bool inspected = await _context.RoomInspections
            .AnyAsync(x => x.BookingId == booking.Id && !x.IsDeleted);
        if (inspected)
        {
            TempData["Error"] = "Booking này đã được kiểm tra phòng và Checkout.";
            return RedirectToAction(nameof(Index));
        }

        var assetItems = await _context.RoomChargeItems
            .Where(x => !x.IsDeleted && x.Category == "Asset")
            .OrderBy(x => x.Id)
            .ToListAsync();

        var bookingRoomIds = booking.BookingDetails
            .Where(x => x.Room != null)
            .Select(x => x.RoomId)
            .Distinct()
            .ToList();

        var roomMiniBarItems = await _context.RoomMiniBarItems
            .Include(x => x.RoomChargeItem)
            .Where(x =>
                !x.IsDeleted &&
                bookingRoomIds.Contains(x.RoomId) &&
                x.RoomChargeItem != null &&
                !x.RoomChargeItem.IsDeleted &&
                x.RoomChargeItem.Category == "MiniBar")
            .OrderBy(x => x.Id)
            .ToListAsync();

        if (model.Rooms == null || model.Rooms.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Không có dữ liệu kiểm tra phòng.");
        }
        else
        {
            if (model.Rooms.GroupBy(x => x.RoomId).Any(x => x.Count() > 1))
            {
                ModelState.AddModelError(string.Empty, "Dữ liệu phòng kiểm tra không hợp lệ.");
            }
            if (model.Rooms.Any(x => !bookingRoomIds.Contains(x.RoomId)))
            {
                ModelState.AddModelError(string.Empty, "Có phòng không thuộc Booking này.");
            }
        }

        var inspectionDetails = new List<RoomInspectionDetail>();
        var miniBarUsage = new Dictionary<int, int>();
        decimal inspectionTotal = 0;

        foreach (var bookingDetail in booking.BookingDetails.Where(x => x.Room != null))
        {
            var roomInput = model.Rooms?
                .FirstOrDefault(x => x.RoomId == bookingDetail.RoomId);
            if (roomInput == null)
            {
                ModelState.AddModelError(string.Empty,
                    $"Thiếu dữ liệu kiểm tra phòng {bookingDetail.Room!.RoomNumber}.");
                continue;
            }

            if (roomInput.Items == null ||
                roomInput.Items.GroupBy(x => new
                {
                    x.RoomChargeItemId,
                    x.RoomMiniBarItemId
                }).Any(x => x.Count() > 1))
            {
                ModelState.AddModelError(string.Empty,
                    $"Dữ liệu kiểm tra phòng {bookingDetail.Room!.RoomNumber} không hợp lệ.");
                continue;
            }

            var miniBarItemsOfRoom = roomMiniBarItems
                .Where(x => x.RoomId == bookingDetail.RoomId)
                .ToList();

            foreach (var miniBarItem in miniBarItemsOfRoom)
            {
                var chargeItem = miniBarItem.RoomChargeItem!;
                var input = roomInput.Items.FirstOrDefault(x =>
                    x.RoomMiniBarItemId == miniBarItem.Id &&
                    x.RoomChargeItemId == chargeItem.Id);

                if (input == null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Thiếu kết quả kiểm tra {chargeItem.Name} tại phòng {bookingDetail.Room!.RoomNumber}.");
                    continue;
                }

                if (input.Quantity < 0)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Số lượng {chargeItem.Name} không hợp lệ.");
                    continue;
                }

                if (input.Quantity > miniBarItem.CurrentQuantity)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Phòng {bookingDetail.Room!.RoomNumber} chỉ còn {miniBarItem.CurrentQuantity} {chargeItem.Name} trong minibar.");
                    continue;
                }

                int quantity = input.Quantity;
                string resultType = quantity > 0 ? "Used" : "Normal";
                decimal unitPrice = quantity > 0 ? chargeItem.UsedPrice : 0;
                decimal amount = unitPrice * quantity;

                inspectionTotal += amount;

                inspectionDetails.Add(new RoomInspectionDetail
                {
                    RoomId = bookingDetail.RoomId,
                    RoomChargeItemId = chargeItem.Id,
                    ItemName = chargeItem.Name,
                    Category = chargeItem.Category,
                    ResultType = resultType,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    Amount = amount
                });

                if (quantity > 0)
                {
                    miniBarUsage[miniBarItem.Id] = quantity;
                }
            }

            foreach (var chargeItem in assetItems)
            {
                var input = roomInput.Items.FirstOrDefault(x =>
                    x.RoomMiniBarItemId == null &&
                    x.RoomChargeItemId == chargeItem.Id);

                if (input == null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Thiếu kết quả kiểm tra {chargeItem.Name} tại phòng {bookingDetail.Room!.RoomNumber}.");
                    continue;
                }

                string resultType = input.ResultType?.Trim() ?? "Normal";
                int quantity;
                decimal unitPrice;
                decimal amount;

                if (resultType != "Normal" &&
                    resultType != "Damaged" &&
                    resultType != "Lost")
                {
                    ModelState.AddModelError(string.Empty,
                        $"Trạng thái {chargeItem.Name} không hợp lệ.");
                    continue;
                }

                if (resultType == "Normal")
                {
                    quantity = 0;
                    unitPrice = 0;
                    amount = 0;
                }
                else
                {
                    if (input.Quantity <= 0 || input.Quantity > 1000)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Vui lòng nhập số lượng {chargeItem.Name} bị hỏng hoặc mất.");
                        continue;
                    }

                    quantity = input.Quantity;
                    unitPrice = resultType == "Damaged"
                        ? chargeItem.DamagedPrice
                        : chargeItem.LostPrice;
                    amount = unitPrice * quantity;
                }

                inspectionTotal += amount;

                inspectionDetails.Add(new RoomInspectionDetail
                {
                    RoomId = bookingDetail.RoomId,
                    RoomChargeItemId = chargeItem.Id,
                    ItemName = chargeItem.Name,
                    Category = chargeItem.Category,
                    ResultType = resultType,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    Amount = amount
                });
            }
        }

        if (!ModelState.IsValid)
        {
            var formModel = await CreateRoomInspectionViewModelAsync(booking, model);
            return View("CheckOut", formModel);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var usage in miniBarUsage)
            {
                var roomMiniBarItem = roomMiniBarItems
                    .First(x => x.Id == usage.Key);

                if (usage.Value > roomMiniBarItem.CurrentQuantity)
                {
                    throw new InvalidOperationException(
                        $"Số lượng {roomMiniBarItem.RoomChargeItem?.Name ?? "minibar"} trong phòng đã thay đổi. Vui lòng kiểm tra lại.");
                }

                roomMiniBarItem.CurrentQuantity -= usage.Value;
                roomMiniBarItem.UpdatedAt = DateTime.Now;
            }

            var inspection = new RoomInspection
            {
                BookingId = booking.Id,
                InspectionDate = DateTime.Now,
                TotalCharge = inspectionTotal,
                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim()
            };

            foreach (var detail in inspectionDetails)
            {
                inspection.RoomInspectionDetails.Add(detail);
            }

            _context.RoomInspections.Add(inspection);

            decimal roomAmount = booking.BookingDetails.Sum(x => x.TotalPrice);
            decimal serviceAmount = booking.ServiceBookings.Sum(x => x.TotalPrice);
            decimal subTotal = roomAmount + serviceAmount + inspectionTotal;
            decimal taxPercent = 8;
            decimal taxAmount = subTotal * taxPercent / 100;
            decimal total = subTotal + taxAmount;

            Invoice invoice;
            if (booking.Invoice == null)
            {
                invoice = new Invoice
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

                AddInvoiceDetails(invoice, booking);
                booking.Invoice = invoice;
                _context.Invoices.Add(invoice);
            }
            else
            {
                invoice = booking.Invoice;
                invoice.RoomAmount = roomAmount;
                invoice.ServiceAmount = serviceAmount;
                invoice.DiscountPercent = 0;
                invoice.TaxPercent = taxPercent;
                invoice.TotalAmount = total;

                if (!invoice.InvoiceDetails.Any(d => !d.IsDeleted))
                {
                    AddInvoiceDetails(invoice, booking);
                }
            }

            foreach (var detail in inspectionDetails.Where(x => x.Amount > 0))
            {
                string roomNumber = booking.BookingDetails
                    .First(x => x.RoomId == detail.RoomId)
                    .Room!.RoomNumber;

                string detailType = detail.Category == "MiniBar"
                    ? "MiniBar"
                    : "Compensation";

                string itemName = detail.Category == "MiniBar"
                    ? $"{detail.ItemName} - Phòng {roomNumber}"
                    : $"{detail.ItemName} - {(detail.ResultType == "Damaged" ? "Hỏng" : "Mất")} - Phòng {roomNumber}";

                invoice.InvoiceDetails.Add(new InvoiceDetail
                {
                    DetailType = detailType,
                    ItemName = itemName,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Amount = detail.Amount,
                    Note = "Kiểm tra phòng trước Checkout"
                });
            }

            booking.Status = BookingStatus.CheckedOut;

            foreach (var detail in booking.BookingDetails)
            {
                if (detail.Room != null)
                {
                    detail.Room.Status = RoomStatus.Available;
                    detail.Room.IsAvailable = true;
                }
            }

            await _activityService.AddForCurrentUserAsync(
                User,
                "CheckOut",
                booking,
                invoice,
                $"Check Out Booking {booking.BookingCode} - Hóa đơn {invoice.InvoiceCode}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = inspectionTotal > 0
                ? $"Checkout thành công. Phụ thu kiểm tra phòng: {inspectionTotal:N0} VNĐ."
                : "Checkout thành công. Không phát sinh phụ thu kiểm tra phòng.";

            return RedirectToAction("Details", "Invoices", new { id = invoice.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(string.Empty,
                ex.InnerException?.Message ?? ex.Message);

            var formModel = await CreateRoomInspectionViewModelAsync(booking, model);
            return View("CheckOut", formModel);
        }
    }

    private async Task<RoomInspectionViewModel> CreateRoomInspectionViewModelAsync(
        Booking booking,
        RoomInspectionViewModel? currentModel = null)
    {
        var assetItems = await _context.RoomChargeItems
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Category == "Asset")
            .OrderBy(x => x.Id)
            .ToListAsync();

        var bookingRoomIds = booking.BookingDetails
            .Where(x => !x.IsDeleted && x.Room != null)
            .Select(x => x.RoomId)
            .Distinct()
            .ToList();

        var roomMiniBarItems = await _context.RoomMiniBarItems
            .AsNoTracking()
            .Include(x => x.RoomChargeItem)
            .Where(x =>
                !x.IsDeleted &&
                bookingRoomIds.Contains(x.RoomId) &&
                x.RoomChargeItem != null &&
                !x.RoomChargeItem.IsDeleted &&
                x.RoomChargeItem.Category == "MiniBar")
            .OrderBy(x => x.Id)
            .ToListAsync();

        var model = new RoomInspectionViewModel
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            CustomerName = booking.Customer?.FullName ?? "Khách hàng",
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            Note = currentModel?.Note
        };

        foreach (var bookingDetail in booking.BookingDetails
            .Where(x => !x.IsDeleted && x.Room != null))
        {
            var currentRoom = currentModel?.Rooms?
                .FirstOrDefault(x => x.RoomId == bookingDetail.RoomId);

            var roomModel = new RoomInspectionRoomViewModel
            {
                RoomId = bookingDetail.RoomId,
                RoomNumber = bookingDetail.Room!.RoomNumber
            };

            foreach (var roomMiniBarItem in roomMiniBarItems
                .Where(x => x.RoomId == bookingDetail.RoomId))
            {
                var chargeItem = roomMiniBarItem.RoomChargeItem!;
                var currentItem = currentRoom?.Items?
                    .FirstOrDefault(x => x.RoomMiniBarItemId == roomMiniBarItem.Id);

                roomModel.Items.Add(new RoomInspectionItemViewModel
                {
                    RoomChargeItemId = chargeItem.Id,
                    RoomMiniBarItemId = roomMiniBarItem.Id,
                    Name = chargeItem.Name,
                    Category = chargeItem.Category,
                    UsedPrice = chargeItem.UsedPrice,
                    DamagedPrice = 0,
                    LostPrice = 0,
                    AvailableQuantity = roomMiniBarItem.CurrentQuantity,
                    Quantity = currentItem?.Quantity ?? 0,
                    ResultType = "Normal"
                });
            }

            foreach (var chargeItem in assetItems)
            {
                var currentItem = currentRoom?.Items?
                    .FirstOrDefault(x =>
                        x.RoomMiniBarItemId == null &&
                        x.RoomChargeItemId == chargeItem.Id);

                roomModel.Items.Add(new RoomInspectionItemViewModel
                {
                    RoomChargeItemId = chargeItem.Id,
                    RoomMiniBarItemId = null,
                    Name = chargeItem.Name,
                    Category = chargeItem.Category,
                    UsedPrice = 0,
                    DamagedPrice = chargeItem.DamagedPrice,
                    LostPrice = chargeItem.LostPrice,
                    AvailableQuantity = 0,
                    Quantity = currentItem?.Quantity ?? 0,
                    ResultType = currentItem?.ResultType ?? "Normal"
                });
            }

            model.Rooms.Add(roomModel);
        }

        return model;
    }

    private void AddInvoiceDetails(Invoice invoice, Booking booking)
    {
        foreach (var detail in booking.BookingDetails.Where(d => !d.IsDeleted))
        {
            invoice.InvoiceDetails.Add(new InvoiceDetail
            {
                DetailType = "Room",
                ItemName = detail.Room?.RoomNumber ?? detail.RoomId.ToString(),
                Quantity = detail.NumberOfNights,
                UnitPrice = detail.PricePerNight,
                Amount = detail.TotalPrice
            });
        }
        foreach (var serviceBooking in booking.ServiceBookings.Where(s => !s.IsDeleted))
        {
            invoice.InvoiceDetails.Add(new InvoiceDetail
            {
                DetailType = "Service",
                ItemName = serviceBooking.Service?.ServiceName ?? "Dịch vụ",
                Quantity = serviceBooking.Quantity,
                UnitPrice = serviceBooking.UnitPrice,
                Amount = serviceBooking.TotalPrice
            });
        }
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

            await _activityService.AddForCurrentUserAsync(
                User,
                "CreateBooking",
                booking,
                null,
                $"Tạo Booking {booking.BookingCode} - Phòng {room.RoomNumber} cho khách {selectedCustomer.FullName}");

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