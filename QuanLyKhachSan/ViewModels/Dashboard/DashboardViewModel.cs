using QuanLyKhachSan.Models;

namespace QuanLyKhachSan.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalRooms { get; set; }

        public int TotalCustomers { get; set; }

        public int TotalEmployees { get; set; }

        public int TotalBookings { get; set; }

        public int AvailableRooms { get; set; }

        public int OccupiedRooms { get; set; }

        public int MaintenanceRooms { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CheckInsToday { get; set; }

        public int CheckOutsToday { get; set; }

        public decimal RevenueToday { get; set; }

        public decimal RevenueThisWeek { get; set; }

        public decimal RevenueThisMonth { get; set; }

        public decimal RevenueThisQuarter { get; set; }

        public decimal ServiceRevenueThisMonth { get; set; }

        public int MyBookings { get; set; }

        public string? FullName { get; set; }

        public string? Role { get; set; }

        public List<string> RevenueWeekLabels { get; set; } = new();

        public List<decimal> RevenueWeekValues { get; set; } = new();

        public List<string> RevenueMonthLabels { get; set; } = new();

        public List<decimal> RevenueMonthValues { get; set; } = new();

        public List<string> RevenueQuarterLabels { get; set; } = new();

        public List<decimal> RevenueQuarterValues { get; set; } = new();

        public List<Booking> RecentBookings { get; set; } = new();

        public List<Room> AvailableRoomList { get; set; } = new();
        public int WeekOffset { get; set; }
        public DateTime SelectedWeekStart { get; set; }
        public DateTime SelectedWeekEnd { get; set; }
        public string SelectedWeekRange => $"{SelectedWeekStart:dd/MM} - " + $"{SelectedWeekEnd.AddDays(-1):dd/MM/yyyy}";

        public bool CanGoToNextWeek => WeekOffset < 0;
    }
}