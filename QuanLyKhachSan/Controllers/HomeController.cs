using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
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
            if (User.IsInRole("Customer"))
            {
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("Admin") && !User.IsInRole("Receptionist"))
            {
                return Forbid();
            }

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime nextMonth = monthStart.AddMonths(1);
            DateTime sevenDayStart = today.AddDays(-6);
            DateTime paymentStart = monthStart < sevenDayStart ? monthStart : sevenDayStart;

            var paidPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.IsDeleted &&
                            p.PaymentStatus == PaymentStatus.Paid &&
                            p.PaymentDate >= paymentStart &&
                            p.PaymentDate < tomorrow)
                .Select(p => new
                {
                    p.PaymentDate,
                    p.Amount
                })
                .ToListAsync();

            var revenueLabels = new List<string>();
            var revenueValues = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                DateTime date = sevenDayStart.AddDays(i);
                revenueLabels.Add(date.ToString("dd/MM"));
                revenueValues.Add(paidPayments
                    .Where(p => p.PaymentDate.Date == date.Date)
                    .Sum(p => p.Amount));
            }

            decimal revenueToday = paidPayments
                .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
                .Sum(p => p.Amount);

            decimal revenueThisMonth = paidPayments
                .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < nextMonth)
                .Sum(p => p.Amount);

            int totalRooms = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => !r.IsDeleted);

            int availableRooms = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => !r.IsDeleted && r.Status == RoomStatus.Available);

            int occupiedRooms = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => !r.IsDeleted && r.Status == RoomStatus.Occupied);

            int maintenanceRooms = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => !r.IsDeleted && r.Status == RoomStatus.Maintenance);

            int pendingBookings = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.Status == BookingStatus.Pending);

            int confirmedBookings = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.Status == BookingStatus.Confirmed);

            var availableRoomList = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .Where(r => !r.IsDeleted && r.Status == RoomStatus.Available)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .Take(8)
                .ToListAsync();

            var recentBookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Room)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.BookingDate)
                .Take(7)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                OccupiedRooms = occupiedRooms,
                MaintenanceRooms = maintenanceRooms,

                TotalCustomers = await _context.Customers
                    .AsNoTracking()
                    .CountAsync(c => !c.IsDeleted),

                TotalEmployees = await _context.Employees
                    .AsNoTracking()
                    .CountAsync(e => !e.IsDeleted),

                TotalBookings = await _context.Bookings
                    .AsNoTracking()
                    .CountAsync(b => !b.IsDeleted),

                PendingBookings = pendingBookings,
                ConfirmedBookings = confirmedBookings,

                CheckInsToday = await _context.Bookings
                    .AsNoTracking()
                    .CountAsync(b => !b.IsDeleted &&
                                     b.CheckInDate >= today &&
                                     b.CheckInDate < tomorrow &&
                                     b.Status != BookingStatus.Cancelled),

                CheckOutsToday = await _context.Bookings
                    .AsNoTracking()
                    .CountAsync(b => !b.IsDeleted &&
                                     b.CheckOutDate >= today &&
                                     b.CheckOutDate < tomorrow &&
                                     b.Status != BookingStatus.Cancelled),

                RevenueToday = revenueToday,
                RevenueThisMonth = revenueThisMonth,

                ServiceRevenueThisMonth = await _context.Invoices
                    .AsNoTracking()
                    .Where(i => !i.IsDeleted &&
                                i.InvoiceDate >= monthStart &&
                                i.InvoiceDate < nextMonth)
                    .SumAsync(i => (decimal?)i.ServiceAmount) ?? 0,

                FullName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name,
                Role = User.IsInRole("Admin") ? "Quản trị viên" : "Lễ tân",

                RevenueLabels = revenueLabels,
                RevenueValues = revenueValues,
                AvailableRoomList = availableRoomList,
                RecentBookings = recentBookings
            };

            ViewBag.PendingBookingCount = model.PendingBookings;

            return User.IsInRole("Admin")
                ? View("Dashboard/Admin", model)
                : View("Dashboard/Receptionist", model);
        }

        public async Task<IActionResult> Index()
        {
            List<Room> rooms = await _context.Rooms
                .AsNoTracking()
                .Include(room => room.RoomType)
                .Include(room => room.RoomImages)
                .Where(room =>
                    !room.IsDeleted &&
                    room.RoomType != null)
                .OrderBy(room => room.RoomType!.Name)
                .ThenBy(room => room.PriceDay)
                .ToListAsync();

            return View(rooms);
        }
    }
}