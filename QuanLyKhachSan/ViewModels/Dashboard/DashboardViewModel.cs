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

        public int MyBookings { get; set; }

        public string? FullName { get; set; }

        public string? Role { get; set; }
    }
}