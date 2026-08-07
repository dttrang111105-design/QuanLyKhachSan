using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;
[Authorize(Roles = "Admin,Receptionist")]
public class RoomChargeItemsController : Controller
{
    private readonly ApplicationDbContext _context;
    public RoomChargeItemsController(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index()
    {
        var items = await _context.RoomChargeItems
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync();
        return View(items);
    }
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var item = await _context.RoomChargeItems
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (item == null)
        {
            return NotFound();
        }
        return View(item);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(
    int id,
    [Bind("Id,UsedPrice,DamagedPrice,LostPrice")] RoomChargeItem model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var item = await _context.RoomChargeItems
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (item == null)
        {
            return NotFound();
        }

        ModelState.Remove(nameof(RoomChargeItem.Name));
        ModelState.Remove(nameof(RoomChargeItem.Category));

        if (model.UsedPrice < 0)
        {
            ModelState.AddModelError(nameof(RoomChargeItem.UsedPrice), "Giá sử dụng không được âm.");
        }

        if (model.DamagedPrice < 0)
        {
            ModelState.AddModelError(nameof(RoomChargeItem.DamagedPrice), "Giá đền bù khi hỏng không được âm.");
        }

        if (model.LostPrice < 0)
        {
            ModelState.AddModelError(nameof(RoomChargeItem.LostPrice), "Giá đền bù khi mất không được âm.");
        }

        if (!ModelState.IsValid)
        {
            model.Name = item.Name;
            model.Category = item.Category;
            model.CreatedAt = item.CreatedAt;
            model.UpdatedAt = item.UpdatedAt;
            model.IsDeleted = item.IsDeleted;

            return View(model);
        }

        if (item.Category == "MiniBar")
        {
            item.UsedPrice = model.UsedPrice;
            item.DamagedPrice = 0;
            item.LostPrice = 0;
        }
        else
        {
            item.UsedPrice = 0;
            item.DamagedPrice = model.DamagedPrice;
            item.LostPrice = model.LostPrice;
        }

        item.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật bảng giá kiểm tra phòng thành công.";

        return RedirectToAction(nameof(Index));
    }
}