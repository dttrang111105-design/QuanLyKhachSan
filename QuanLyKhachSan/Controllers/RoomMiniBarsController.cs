using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels;

namespace QuanLyKhachSan.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class RoomMiniBarsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomMiniBarsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .Include(r => r.RoomMiniBarItems.Where(item => !item.IsDeleted))
                    .ThenInclude(item => item.RoomChargeItem)
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            return View(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> Manage(int id)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (room == null)
            {
                return NotFound();
            }

            var miniBarItems = await _context.RoomChargeItems
                .AsNoTracking()
                .Where(item => !item.IsDeleted && item.Category == "MiniBar")
                .OrderBy(item => item.Name)
                .ToListAsync();

            var roomItems = await _context.RoomMiniBarItems
                .AsNoTracking()
                .Where(item => !item.IsDeleted && item.RoomId == id)
                .ToDictionaryAsync(item => item.RoomChargeItemId);

            var model = new RoomMiniBarViewModel
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Items = miniBarItems.Select(item =>
                {
                    roomItems.TryGetValue(item.Id, out var roomItem);

                    return new RoomMiniBarItemViewModel
                    {
                        Id = roomItem?.Id,
                        RoomChargeItemId = item.Id,
                        Name = item.Name,
                        UsedPrice = item.UsedPrice,
                        IsEnabled = roomItem != null,
                        StandardQuantity = roomItem?.StandardQuantity ?? 0,
                        CurrentQuantity = roomItem?.CurrentQuantity ?? 0
                    };
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(RoomMiniBarViewModel model)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == model.RoomId && !r.IsDeleted);

            if (room == null)
            {
                return NotFound();
            }

            var miniBarItems = await _context.RoomChargeItems
                .Where(item => !item.IsDeleted && item.Category == "MiniBar")
                .ToListAsync();

            var validItemIds = miniBarItems
                .Select(item => item.Id)
                .ToHashSet();

            foreach (var item in model.Items)
            {
                if (!validItemIds.Contains(item.RoomChargeItemId))
                {
                    ModelState.AddModelError(string.Empty, "Có mặt hàng minibar không hợp lệ.");
                    continue;
                }

                if (item.StandardQuantity < 0)
                {
                    ModelState.AddModelError(string.Empty, "Số lượng chuẩn không được âm.");
                }

                if (item.CurrentQuantity < 0)
                {
                    ModelState.AddModelError(string.Empty, "Số lượng hiện có không được âm.");
                }

                if (item.CurrentQuantity > item.StandardQuantity)
                {
                    ModelState.AddModelError(string.Empty, "Số lượng hiện có không được lớn hơn số lượng chuẩn.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.RoomNumber = room.RoomNumber;

                var priceById = miniBarItems.ToDictionary(item => item.Id);

                foreach (var item in model.Items)
                {
                    if (priceById.TryGetValue(item.RoomChargeItemId, out var priceItem))
                    {
                        item.Name = priceItem.Name;
                        item.UsedPrice = priceItem.UsedPrice;
                    }
                }

                return View(model);
            }

            var existingItems = await _context.RoomMiniBarItems
                .Where(item => item.RoomId == model.RoomId)
                .ToListAsync();

            foreach (var item in model.Items)
            {
                var existingItem = existingItems
                    .FirstOrDefault(x => x.RoomChargeItemId == item.RoomChargeItemId);

                if (!item.IsEnabled)
                {
                    if (existingItem != null && !existingItem.IsDeleted)
                    {
                        existingItem.IsDeleted = true;
                        existingItem.UpdatedAt = DateTime.Now;
                    }

                    continue;
                }

                if (existingItem == null)
                {
                    existingItem = new RoomMiniBarItem
                    {
                        RoomId = model.RoomId,
                        RoomChargeItemId = item.RoomChargeItemId,
                        StandardQuantity = item.StandardQuantity,
                        CurrentQuantity = item.CurrentQuantity,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };

                    _context.RoomMiniBarItems.Add(existingItem);
                }
                else
                {
                    existingItem.StandardQuantity = item.StandardQuantity;
                    existingItem.CurrentQuantity = item.CurrentQuantity;
                    existingItem.IsDeleted = false;
                    existingItem.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cập nhật minibar phòng {room.RoomNumber} thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refill(int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (room == null)
            {
                return NotFound();
            }

            var items = await _context.RoomMiniBarItems
                .Where(item => item.RoomId == id && !item.IsDeleted)
                .ToListAsync();

            foreach (var item in items)
            {
                item.CurrentQuantity = item.StandardQuantity;
                item.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã bổ sung đầy minibar phòng {room.RoomNumber}.";

            return RedirectToAction(nameof(Index));
        }
    }
}