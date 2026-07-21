
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

    // GET: ROOMTYPES
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

    // GET: ROOMTYPES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roomtype = await _context.RoomTypes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (roomtype == null)
        {
            return NotFound();
        }

        return View(roomtype);
    }

    // GET: ROOMTYPES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ROOMTYPES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,BasePrice,MaxOccupancy,BedType,Area,Description")] RoomType roomtype)
    {
        if (ModelState.IsValid)
        {
            roomtype.UpdatedAt = DateTime.Now;
            _context.Add(roomtype);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(roomtype);
    }

    // GET: ROOMTYPES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roomtype = await _context.RoomTypes.FindAsync(id);
        if (roomtype == null)
        {
            return NotFound();
        }
        return View(roomtype);
    }

    // POST: ROOMTYPES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Name,BasePrice,MaxOccupancy,BedType,Area,Description,Rooms,Id,CreatedAt,UpdatedAt,IsDeleted")] RoomType roomtype)
    {
        if (id != roomtype.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(roomtype);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomTypeExists(roomtype.Id))
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
        return View(roomtype);
    }

    // GET: ROOMTYPES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var roomtype = await _context.RoomTypes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (roomtype == null)
        {
            return NotFound();
        }

        return View(roomtype);
    }

    // POST: ROOMTYPES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var roomtype = await _context.RoomTypes.FindAsync(id);
        if (roomtype != null)
        {
            _context.RoomTypes.Remove(roomtype);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RoomTypeExists(int? id)
    {
        return _context.RoomTypes.Any(e => e.Id == id);
    }
}
