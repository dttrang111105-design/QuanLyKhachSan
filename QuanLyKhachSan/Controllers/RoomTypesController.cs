using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;

[Authorize(Roles = "Admin,Receptionist")]
public class RoomTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoomTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Index()
    {
        var roomTypes = await _context.RoomTypes
            .AsNoTracking()
            .Include(roomType =>
                roomType.Rooms.Where(room =>
                    !room.IsDeleted))
            .Where(roomType =>
                !roomType.IsDeleted)
            .OrderBy(roomType =>
                roomType.Name)
            .ToListAsync();

        return View(roomTypes);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roomType = await _context.RoomTypes
            .AsNoTracking()
            .Include(x => x.Rooms.Where(room => !room.IsDeleted))
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (roomType == null)
        {
            return NotFound();
        }

        return View(roomType);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new RoomType
        {
            MaxOccupancy = 1,
            Area = 1
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,BasePrice,MaxOccupancy,BedType,Area,Description")]
        RoomType model)
    {
        NormalizeRoomType(model);
        ValidateRoomType(model);

        bool nameExists = await _context.RoomTypes
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.Name == model.Name);

        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(RoomType.Name),
                "Tên loại phòng này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;
        model.IsDeleted = false;

        _context.RoomTypes.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Thêm loại phòng {model.Name} thành công.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roomType = await _context.RoomTypes
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (roomType == null)
        {
            return NotFound();
        }

        return View(roomType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,BasePrice,MaxOccupancy,BedType,Area,Description")]
        RoomType model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        NormalizeRoomType(model);
        ValidateRoomType(model);

        var roomType = await _context.RoomTypes
            .Include(x => x.Rooms.Where(room => !room.IsDeleted))
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (roomType == null)
        {
            return NotFound();
        }

        bool nameExists = await _context.RoomTypes
            .AnyAsync(x =>
                x.Id != id &&
                !x.IsDeleted &&
                x.Name == model.Name);

        if (nameExists)
        {
            ModelState.AddModelError(
                nameof(RoomType.Name),
                "Tên loại phòng này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        DateTime updatedAt = DateTime.Now;

        roomType.Name = model.Name;
        roomType.BasePrice = model.BasePrice;
        roomType.MaxOccupancy = model.MaxOccupancy;
        roomType.BedType = model.BedType;
        roomType.Area = model.Area;
        roomType.Description = model.Description;
        roomType.UpdatedAt = updatedAt;

        // Trang khách hàng, tìm phòng và đặt phòng đều đọc Room.PriceDay.
        // Vì vậy mỗi lần lưu loại phòng, hệ thống luôn đồng bộ BasePrice
        // xuống tất cả phòng đang hoạt động thuộc loại này.
        // Việc luôn đồng bộ (không chỉ khi BasePrice vừa thay đổi) còn sửa được
        // dữ liệu cũ đã bị lệch trước khi phần code này được bổ sung.
        foreach (Room room in roomType.Rooms)
        {
            decimal oldPriceDay = room.PriceDay;
            decimal oldPriceWeek = room.PriceWeek;

            room.PriceDay = model.BasePrice;

            // Giữ nguyên tỷ lệ giá tuần hiện có của từng phòng.
            // Nếu dữ liệu cũ chưa có giá ngày hợp lệ thì giữ nguyên giá tuần.
            if (oldPriceDay > 0 && oldPriceWeek > 0)
            {
                decimal weeklyMultiplier = oldPriceWeek / oldPriceDay;

                room.PriceWeek = decimal.Round(
                    model.BasePrice * weeklyMultiplier,
                    0,
                    MidpointRounding.AwayFromZero);
            }

            room.UpdatedAt = updatedAt;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Cập nhật loại phòng {roomType.Name} và đồng bộ giá cho {roomType.Rooms.Count} phòng thành công.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roomType = await _context.RoomTypes
            .AsNoTracking()
            .Include(x => x.Rooms.Where(room => !room.IsDeleted))
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (roomType == null)
        {
            return NotFound();
        }

        return View(roomType);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var roomType = await _context.RoomTypes
            .Include(x => x.Rooms.Where(room => !room.IsDeleted))
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (roomType == null)
        {
            return NotFound();
        }

        if (roomType.Rooms.Any())
        {
            TempData["Error"] =
                "Không thể xóa loại phòng đang có phòng sử dụng.";

            return RedirectToAction(nameof(Index));
        }

        roomType.IsDeleted = true;
        roomType.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Xóa loại phòng {roomType.Name} thành công.";

        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeRoomType(RoomType model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.BedType = model.BedType?.Trim();
        model.Description = model.Description?.Trim();
    }

    private void ValidateRoomType(RoomType model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(
                nameof(RoomType.Name),
                "Vui lòng nhập tên loại phòng.");
        }

        if (model.BasePrice < 0)
        {
            ModelState.AddModelError(
                nameof(RoomType.BasePrice),
                "Giá cơ bản không được âm.");
        }

        if (model.MaxOccupancy <= 0)
        {
            ModelState.AddModelError(
                nameof(RoomType.MaxOccupancy),
                "Sức chứa phải lớn hơn 0.");
        }

        if (model.Area <= 0)
        {
            ModelState.AddModelError(
                nameof(RoomType.Area),
                "Diện tích phải lớn hơn 0.");
        }
    }

    private bool RoomTypeExists(int id)
    {
        return _context.RoomTypes.Any(x =>
            x.Id == id &&
            !x.IsDeleted);
    }
}