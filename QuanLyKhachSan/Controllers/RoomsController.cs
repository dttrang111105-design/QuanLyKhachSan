
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;

public class RoomsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: ROOMS
    public async Task<IActionResult> Index()
    {
        var rooms = await _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomImages)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        return View(rooms);
    }

    // GET: ROOMS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var room = await _context.Rooms
            .Include(r => r.RoomType)
            .Include(r => r.RoomImages)
            .Include(r => r.BookingDetails)
                .ThenInclude(b => b.Booking)
            .FirstOrDefaultAsync(r => r.Id == id);

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

        var room = await _context.Rooms.FindAsync(id);
        if (room == null)
        {
            return NotFound();
        }
        return View(room);
    }

    // POST: ROOMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("RoomTypeId,RoomType,RoomNumber,Floor,Status,PriceDay,PriceWeek,RoomSize,MaxOccupancy,Description,IsAvailable,RoomImages,BookingDetails,Reviews,Id,CreatedAt,UpdatedAt,IsDeleted")] Room room)
    {
        if (id != room.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                room.IsAvailable = room.Status == RoomStatus.Available;
                _context.Update(room);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(room.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(room);
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

    private bool RoomExists(int? id)
    {
        return _context.Rooms.Any(e => e.Id == id);
    }
}
