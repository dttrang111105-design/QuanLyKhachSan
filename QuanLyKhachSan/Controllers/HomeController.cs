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

        [HttpGet]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> GetWeeklyRevenue(int weekOffset = 0)
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            //xem tối đa 260 tuần trước
            weekOffset = Math.Clamp(weekOffset, -260, 0);
            // Tìm thứ Hai của tuần hiện tại.
            int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
            DateTime currentWeekStart = today.AddDays(-daysFromMonday);
            DateTime selectedWeekStart = currentWeekStart.AddDays(weekOffset * 7);
            DateTime selectedWeekEnd = selectedWeekStart.AddDays(7);
            // Tuần hiện tại không lấy dữ liệu của ngày tương lai.
            DateTime queryEnd = selectedWeekEnd > tomorrow ? tomorrow : selectedWeekEnd;
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(payment =>
                    !payment.IsDeleted &&
                    payment.PaymentStatus == PaymentStatus.Paid &&
                    payment.PaymentDate >= selectedWeekStart &&
                    payment.PaymentDate < queryEnd)
                .Select(payment => new
                {
                    payment.PaymentDate,
                    payment.Amount
                })
                .ToListAsync();
            string[] vietnameseWeekDays =
            {
                "CN", "T2", "T3", "T4", "T5", "T6", "T7"
            };
            var labels = new List<string>();
            var values = new List<decimal>();
            for (int i = 0; i < 7; i++)
            {
                DateTime date = selectedWeekStart.AddDays(i);
                labels.Add($"{vietnameseWeekDays[(int)date.DayOfWeek]} {date:dd/MM}");
                decimal revenueOfDay = payments
                    .Where(payment => payment.PaymentDate.Date == date.Date)
                    .Sum(payment => payment.Amount);
                values.Add(revenueOfDay);
            }
            decimal total = values.Sum();
            string selectedWeekRange = $"{selectedWeekStart:dd/MM} - " + $"{selectedWeekEnd.AddDays(-1):dd/MM/yyyy}";
            return Json(new
            {
                weekOffset,
                selectedWeekRange,
                labels,
                values,
                total,

                canGoToPreviousWeek = weekOffset > -260,
                canGoToNextWeek = weekOffset < 0
            });
        }

        [Authorize]
        public async Task<IActionResult> Dashboard(int weekOffset = 0)
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
            //xem tối đa 260 tuần trước.
            weekOffset = Math.Clamp(weekOffset, -260, 0);
            // Tháng hiện tại
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime nextMonth = monthStart.AddMonths(1);
            // Xác định thứ Hai của tuần hiện tại
            int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
            DateTime currentWeekStart = today.AddDays(-daysFromMonday);
            // Tuần đang chọn
            DateTime selectedWeekStart = currentWeekStart.AddDays(weekOffset * 7);
            DateTime selectedWeekEnd = selectedWeekStart.AddDays(7);
            //không lấy dữ liệu tương lai
            DateTime selectedWeekQueryEnd = selectedWeekEnd > tomorrow ? tomorrow : selectedWeekEnd;

            // Quý hiện tại
            int quarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
            DateTime quarterStart = new(today.Year, quarterStartMonth, 1);
            DateTime nextQuarter = quarterStart.AddMonths(3);
            // Dữ liệu riêng của tuần đang chọn
            var selectedWeekPayments = await _context.Payments
                .AsNoTracking()
                .Where(payment =>
                    !payment.IsDeleted &&
                    payment.PaymentStatus == PaymentStatus.Paid &&
                    payment.PaymentDate >= selectedWeekStart &&
                    payment.PaymentDate < selectedWeekQueryEnd)
                .Select(payment => new
                {
                    payment.PaymentDate,
                    payment.Amount
                })
                .ToListAsync();

            // Dữ liệu của tháng và quý hiện tại
            var currentPeriodPayments = await _context.Payments
                .AsNoTracking()
                .Where(payment =>
                    !payment.IsDeleted &&
                    payment.PaymentStatus == PaymentStatus.Paid &&
                    payment.PaymentDate >= quarterStart &&
                    payment.PaymentDate < tomorrow)
                .Select(payment => new
                {
                    payment.PaymentDate,
                    payment.Amount
                })
                .ToListAsync();

            // DOANH THU THEO TUẦN
            var revenueWeekLabels = new List<string>();
            var revenueWeekValues = new List<decimal>();

            string[] vietnameseWeekDays =
            {
                "CN", "T2", "T3", "T4", "T5", "T6", "T7"
            };

            for (int i = 0; i < 7; i++)
            {
                DateTime date = selectedWeekStart.AddDays(i);
                revenueWeekLabels.Add($"{vietnameseWeekDays[(int)date.DayOfWeek]} {date:dd/MM}");
                decimal revenueOfDay = selectedWeekPayments
                    .Where(payment => payment.PaymentDate.Date == date.Date)
                    .Sum(payment => payment.Amount);
                revenueWeekValues.Add(revenueOfDay);
            }

            // DOANH THU THEO THÁNG
            var revenueMonthLabels = new List<string>();
            var revenueMonthValues = new List<decimal>();

            int daysInMonth = DateTime.DaysInMonth( today.Year, today.Month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new(today.Year, today.Month, day);
                revenueMonthLabels.Add(date.ToString("dd/MM"));
                decimal revenueOfDay = currentPeriodPayments
                    .Where(p => p.PaymentDate.Date == date.Date)
                    .Sum(p => p.Amount);

                revenueMonthValues.Add(revenueOfDay);
            }
            // DOANH THU THEO QUÝ
            // Mỗi cột tương ứng một tháng
            var revenueQuarterLabels = new List<string>();
            var revenueQuarterValues = new List<decimal>();

            for (int i = 0; i < 3; i++)
            {
                DateTime periodStart = quarterStart.AddMonths(i);
                DateTime periodEnd = periodStart.AddMonths(1);
                revenueQuarterLabels.Add($"Tháng {periodStart.Month}");
                decimal revenueOfMonth = currentPeriodPayments
                    .Where(payment =>payment.PaymentDate >= periodStart && payment.PaymentDate < periodEnd)
                    .Sum(payment => payment.Amount);
                revenueQuarterValues.Add(revenueOfMonth);
            }

            // Tổng doanh thu hôm nay
            decimal revenueToday = currentPeriodPayments
                .Where(payment =>
                    payment.PaymentDate >= today &&
                    payment.PaymentDate < tomorrow)
                .Sum(payment => payment.Amount);

            // Đây là tổng của tuần người dùng đang chọn
            decimal revenueThisWeek = selectedWeekPayments
                .Sum(payment => payment.Amount);

            decimal revenueThisMonth = currentPeriodPayments
                .Where(payment =>
                    payment.PaymentDate >= monthStart &&
                    payment.PaymentDate < nextMonth)
                .Sum(payment => payment.Amount);

            decimal revenueThisQuarter = currentPeriodPayments
                .Where(payment =>
                    payment.PaymentDate >= quarterStart &&
                    payment.PaymentDate < nextQuarter)
                .Sum(payment => payment.Amount);

            // Thống kê phòng
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

            // Thống kê đặt phòng
            int pendingBookings = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.Status == BookingStatus.Pending);

            int confirmedBookings = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.Status == BookingStatus.Confirmed);

            // Danh sách phòng trống
            var availableRoomList = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.RoomType)
                .Where(r => !r.IsDeleted && r.Status == RoomStatus.Available)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .Take(8)
                .ToListAsync();

            // Danh sách đặt phòng gần đây
            var recentBookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails
                    .Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Room)
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.BookingDate)
                .Take(7)
                .ToListAsync();
            int totalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync(customer => !customer.IsDeleted);

            int totalEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync(employee => !employee.IsDeleted);

            int totalBookings = await _context.Bookings
                .AsNoTracking()
                .CountAsync(booking => !booking.IsDeleted);

            int checkInsToday = await _context.Bookings
                .AsNoTracking()
                .CountAsync(booking =>
                    !booking.IsDeleted &&
                    booking.CheckInDate >= today &&
                    booking.CheckInDate < tomorrow &&
                    booking.Status != BookingStatus.Cancelled);

            int checkOutsToday = await _context.Bookings
                .AsNoTracking()
                .CountAsync(booking =>
                    !booking.IsDeleted &&
                    booking.CheckOutDate >= today &&
                    booking.CheckOutDate < tomorrow &&
                    booking.Status != BookingStatus.Cancelled);

            decimal serviceRevenueThisMonth = await _context.Invoices
                .AsNoTracking()
                .Where(invoice =>
                    !invoice.IsDeleted &&
                    invoice.InvoiceDate >= monthStart &&
                    invoice.InvoiceDate < nextMonth)
                .SumAsync(invoice => (decimal?)invoice.ServiceAmount) ?? 0;

            string fullName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "Quản trị viên";
            var model = new DashboardViewModel
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                OccupiedRooms = occupiedRooms,
                MaintenanceRooms = maintenanceRooms,

                TotalCustomers = totalCustomers,
                TotalEmployees = totalEmployees,
                TotalBookings = totalBookings,

                PendingBookings = pendingBookings,
                ConfirmedBookings = confirmedBookings,

                CheckInsToday = checkInsToday,
                CheckOutsToday = checkOutsToday,

                RevenueToday = revenueToday,
                RevenueThisWeek = revenueThisWeek,
                RevenueThisMonth = revenueThisMonth,
                RevenueThisQuarter = revenueThisQuarter,

                WeekOffset = weekOffset,
                SelectedWeekStart = selectedWeekStart,
                SelectedWeekEnd = selectedWeekEnd,

                ServiceRevenueThisMonth = serviceRevenueThisMonth,

                FullName = fullName,

                Role = User.IsInRole("Admin") ? "Quản trị viên" : "Lễ tân",

                RevenueWeekLabels = revenueWeekLabels,
                RevenueWeekValues = revenueWeekValues,

                RevenueMonthLabels = revenueMonthLabels,
                RevenueMonthValues = revenueMonthValues,

                RevenueQuarterLabels = revenueQuarterLabels,
                RevenueQuarterValues = revenueQuarterValues,

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
                .Where(room => !room.IsDeleted && room.RoomType != null)
                .OrderBy(room => room.RoomType!.Name)
                .ThenBy(room => room.PriceDay)
                .ToListAsync();
            return View(rooms);
        }
    }
}