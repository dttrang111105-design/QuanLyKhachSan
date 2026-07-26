using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels.Rooms;

public class RoomsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ROOMS
    public async Task<IActionResult> Index(RoomStatus? status)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(room => room.RoomType)
            .Include(room => room.RoomImages)
            .Where(room => !room.IsDeleted);
        if (status.HasValue)
        {
            query = query.Where(room => room.Status == status.Value);
        }
        var rooms = await query
            .OrderBy(room => room.Floor)
            .ThenBy(room => room.RoomNumber)
            .ToListAsync();
        ViewBag.SelectedStatus = status;
        ViewBag.TotalRoomCount = await _context.Rooms.AsNoTracking().CountAsync(room => !room.IsDeleted);
        ViewBag.AvailableCount = await _context.Rooms.AsNoTracking().CountAsync(room => !room.IsDeleted && room.Status == RoomStatus.Available);
        ViewBag.OccupiedCount = await _context.Rooms.AsNoTracking().CountAsync(room => !room.IsDeleted && room.Status == RoomStatus.Occupied);
        ViewBag.MaintenanceCount = await _context.Rooms.AsNoTracking().CountAsync(room => !room.IsDeleted && room.Status == RoomStatus.Maintenance);
        return View(rooms);
    }

    // GET: ROOMS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.RoomImages)
            .Include(r => r.BookingDetails)
                .ThenInclude(b => b.Booking)
            .Include(r => r.Reviews.Where(review => !review.IsDeleted))
                .ThenInclude(review => review.Customer)
            .FirstOrDefaultAsync(r =>
                r.Id == id &&
                !r.IsDeleted);

        if (room == null)
            return NotFound();

        return View(room);
    }

    // GET: Rooms/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var roomTypes = await _context.RoomTypes
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewData["RoomTypeId"] = new SelectList(
            roomTypes,
            "Id",
            "Name");

        return View(new Room
        {
            Status = RoomStatus.Available,
            Floor = 1,
            MaxOccupancy = 1,
            RoomSize = 1
        });
    }

    // POST: ROOMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind(
        "RoomTypeId,RoomNumber,Floor,Status,PriceDay," +
        "PriceWeek,RoomSize,MaxOccupancy,Description")]
    Room room)
    {
        room.RoomNumber =
            room.RoomNumber?.Trim() ?? string.Empty;

        var roomNumberExists = await _context.Rooms
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.RoomNumber == room.RoomNumber);

        if (roomNumberExists)
        {
            ModelState.AddModelError(
                nameof(Room.RoomNumber),
                "Số phòng này đã tồn tại.");
        }

        var roomTypeExists = await _context.RoomTypes
            .AnyAsync(x =>
                x.Id == room.RoomTypeId &&
                !x.IsDeleted);

        if (!roomTypeExists)
        {
            ModelState.AddModelError(
                nameof(Room.RoomTypeId),
                "Loại phòng không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            var roomTypes = await _context.RoomTypes
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewData["RoomTypeId"] = new SelectList(
                roomTypes,
                "Id",
                "Name",
                room.RoomTypeId);

            return View(room);
        }

        room.IsAvailable =
            room.Status == RoomStatus.Available;

        room.CreatedAt = DateTime.Now;
        room.IsDeleted = false;

        _context.Rooms.Add(room);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Thêm phòng {room.RoomNumber} thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: ROOMS/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        await LoadRoomTypesAsync(room.RoomTypeId);

        return View(room);
    }

    // POST: ROOMS/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,RoomTypeId,RoomNumber,Floor,Status,PriceDay," +
            "PriceWeek,RoomSize,MaxOccupancy,Description")]
        Room model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        model.RoomNumber =
            model.RoomNumber?.Trim() ?? string.Empty;

        var room = await _context.Rooms
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (room == null)
        {
            return NotFound();
        }

        bool roomNumberExists = await _context.Rooms
            .AnyAsync(x =>
                x.Id != id &&
                !x.IsDeleted &&
                x.RoomNumber == model.RoomNumber);

        if (roomNumberExists)
        {
            ModelState.AddModelError(
                nameof(Room.RoomNumber),
                "Số phòng này đã tồn tại.");
        }

        bool roomTypeExists = await _context.RoomTypes
            .AnyAsync(x =>
                x.Id == model.RoomTypeId &&
                !x.IsDeleted);

        if (!roomTypeExists)
        {
            ModelState.AddModelError(
                nameof(Room.RoomTypeId),
                "Loại phòng không hợp lệ.");
        }

        if (model.PriceDay < 0 ||
            model.PriceWeek < 0 ||
            model.RoomSize <= 0 ||
            model.MaxOccupancy <= 0 ||
            model.Floor <= 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Vui lòng kiểm tra lại tầng, giá phòng, diện tích và sức chứa.");
        }

        if (!ModelState.IsValid)
        {
            await LoadRoomTypesAsync(model.RoomTypeId);
            return View(model);
        }

        room.RoomTypeId = model.RoomTypeId;
        room.RoomNumber = model.RoomNumber;
        room.Floor = model.Floor;
        room.Status = model.Status;
        room.PriceDay = model.PriceDay;
        room.PriceWeek = model.PriceWeek;
        room.RoomSize = model.RoomSize;
        room.MaxOccupancy = model.MaxOccupancy;
        room.Description = model.Description?.Trim();
        room.IsAvailable =
            model.Status == RoomStatus.Available;
        room.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Cập nhật phòng {room.RoomNumber} thành công.";

        return RedirectToAction(nameof(Index));
    }

    // GET: ROOMS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .FirstOrDefaultAsync(m => m.Id == id);
        if (room == null)
        {
            return NotFound();
        }

        return View(room);
    }

    // POST: ROOMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room != null)
        {
            _context.Rooms.Remove(room);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRoomTypesAsync(
        int selectedRoomTypeId)
    {
        var roomTypes = await _context.RoomTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewData["RoomTypeId"] = new SelectList(
            roomTypes,
            "Id",
            "Name",
            selectedRoomTypeId);
    }

    private bool RoomExists(int? id)
    {
        return _context.Rooms.Any(e => e.Id == id);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search(RoomSearchViewModel model)
    {
        // Chỉ lấy phần ngày, loại bỏ giờ phút giây.
        model.CheckInDate = model.CheckInDate.Date;
        model.CheckOutDate = model.CheckOutDate.Date;

        // Xóa khoảng trắng thừa trong từ khóa.
        model.Keyword = model.Keyword?.Trim();

        // Đánh dấu người dùng đã thực hiện tìm kiếm.
        model.HasSearched = true;

        // Lấy danh sách loại phòng để đưa ra combobox.
        model.RoomTypes = await _context.RoomTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Kiểm tra ngày nhận phòng.
        if (model.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.CheckInDate),
                "Ngày nhận phòng không được nhỏ hơn ngày hiện tại."
            );
        }

        // Kiểm tra ngày trả phòng.
        if (model.CheckOutDate <= model.CheckInDate)
        {
            ModelState.AddModelError(
                nameof(model.CheckOutDate),
                "Ngày trả phòng phải lớn hơn ngày nhận phòng."
            );
        }

        // Kiểm tra khoảng giá.
        if (model.MinPrice.HasValue &&
            model.MaxPrice.HasValue &&
            model.MinPrice.Value > model.MaxPrice.Value)
        {
            ModelState.AddModelError(
                nameof(model.MaxPrice),
                "Giá tối đa phải lớn hơn hoặc bằng giá tối thiểu."
            );
        }

        // Nếu dữ liệu không hợp lệ thì trả lại giao diện.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Tổng số khách.
        int totalGuests = model.Adult + model.Children;

        // Truy vấn các phòng chưa bị xóa, không bảo trì
        // và có sức chứa phù hợp.
        var query = _context.Rooms
            .AsNoTracking()
            .Include(r => r.RoomType)
            .Include(r => r.RoomImages)
            .Include(r => r.Reviews.Where(x => !x.IsDeleted))
            .Where(r =>
                !r.IsDeleted &&
                r.Status != RoomStatus.Maintenance &&
                r.MaxOccupancy >= totalGuests
            );

        // Lọc theo loại phòng.
        if (model.RoomTypeId.HasValue)
        {
            query = query.Where(
                r => r.RoomTypeId == model.RoomTypeId.Value
            );
        }

        // Lọc theo giá tối thiểu.
        if (model.MinPrice.HasValue)
        {
            query = query.Where(
                r => r.PriceDay >= model.MinPrice.Value
            );
        }

        // Lọc theo giá tối đa.
        if (model.MaxPrice.HasValue)
        {
            query = query.Where(
                r => r.PriceDay <= model.MaxPrice.Value
            );
        }

        // Tìm theo số phòng, tên loại phòng hoặc mô tả.
        if (!string.IsNullOrWhiteSpace(model.Keyword))
        {
            string keyword = model.Keyword;

            query = query.Where(r =>
                r.RoomNumber.Contains(keyword) ||
                (r.RoomType != null &&
                    r.RoomType.Name.Contains(keyword)) ||
                (r.Description != null &&
                    r.Description.Contains(keyword))
            );
        }

        /*
         * Loại bỏ các phòng đã có Booking trùng lịch.
         *
         * Hai khoảng ngày bị trùng khi:
         * Ngày nhận mới < ngày trả cũ
         * và ngày trả mới > ngày nhận cũ.
         */
        query = query.Where(r =>
            !r.BookingDetails.Any(detail =>
                !detail.IsDeleted &&
                detail.Booking != null &&
                !detail.Booking.IsDeleted &&
                detail.Booking.Status != BookingStatus.Cancelled &&
                detail.Booking.Status != BookingStatus.CheckedOut &&
                model.CheckInDate < detail.Booking.CheckOutDate &&
                model.CheckOutDate > detail.Booking.CheckInDate
            )
        );

        // Lấy kết quả.
        model.Rooms = await query
            .OrderBy(r => r.PriceDay)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();

        return View(model);
    }
}