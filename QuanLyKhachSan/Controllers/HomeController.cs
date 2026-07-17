using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.ViewModels.Dashboard;
using System.Security.Claims;

namespace QuanLyKhachSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                TotalRooms = await _context.Rooms
                    .CountAsync(r => !r.IsDeleted),

                TotalCustomers = await _context.Customers
                    .CountAsync(c => !c.IsDeleted),

                TotalEmployees = await _context.Employees
                    .CountAsync(e => !e.IsDeleted),

                TotalBookings = await _context.Bookings
                    .CountAsync(b => !b.IsDeleted),

                AvailableRooms = await _context.Rooms
                    .CountAsync(r =>
                        !r.IsDeleted &&
                        r.Status == RoomStatus.Available),

                OccupiedRooms = await _context.Rooms
                    .CountAsync(r =>
                        !r.IsDeleted &&
                        r.Status == RoomStatus.Occupied),

                MaintenanceRooms = await _context.Rooms
                    .CountAsync(r =>
                        !r.IsDeleted &&
                        r.Status == RoomStatus.Maintenance)
            };

            if (User.IsInRole("Customer"))
            {
                var accountIdText = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (int.TryParse(accountIdText, out int accountId))
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            c.AccountId == accountId &&
                            !c.IsDeleted);

                    if (customer != null)
                    {
                        model.MyBookings =
                            await _context.Bookings.CountAsync(b =>
                                b.CustomerId == customer.Id &&
                                !b.IsDeleted);
                    }
                }
            }

            if (User.IsInRole("Admin"))
            {
                return View("Dashboard/Admin", model);
            }

            if (User.IsInRole("Receptionist"))
            {
                return View("Dashboard/Receptionist", model);
            }

            return View("Dashboard/Customer", model);
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.RoomImages)
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.PriceDay)
                .Take(6)
                .ToListAsync();

            return View(rooms);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}