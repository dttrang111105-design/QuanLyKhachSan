namespace QuanLyKhachSan.ViewModels
{
    public class ReceptionistActivityViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? EmployeeId { get; set; }
        public string? ActionType { get; set; }

        public int TotalActivities { get; set; }
        public int CreateBookingCount { get; set; }
        public int CheckInCount { get; set; }
        public int CheckOutCount { get; set; }
        public int PaymentCount { get; set; }
        public int CancelBookingCount { get; set; }

        // Tổng doanh thu Paid trong bộ lọc.
        // Nếu chọn 1 nhân viên, đây là doanh thu được gắn với nhân viên đó.
        public decimal TotalRevenue { get; set; }

        // Doanh thu Paid đã xác định được nhân viên trực tiếp xử lý.
        public decimal StaffHandledRevenue { get; set; }

        // Các giao dịch Paid chưa xác định được lễ tân:
        // ví dụ khách tự thanh toán hoặc dữ liệu cũ chưa có Activity liên kết.
        public decimal UnassignedRevenue { get; set; }

        public List<ReceptionistActivityEmployeeOptionViewModel> Employees { get; set; } = new();
        public List<ReceptionistActivitySummaryViewModel> Summaries { get; set; } = new();
        public List<ReceptionistActivityRowViewModel> Activities { get; set; } = new();
        public List<ReceptionistDailyRevenueViewModel> DailyRevenues { get; set; } = new();
        public List<ReceptionistPaymentRevenueViewModel> RevenuePayments { get; set; } = new();
    }

    public class ReceptionistActivityEmployeeOptionViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
    }

    public class ReceptionistActivitySummaryViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int CreateBookingCount { get; set; }
        public int CheckInCount { get; set; }
        public int CheckOutCount { get; set; }
        public int PaymentCount { get; set; }
        public int CancelBookingCount { get; set; }
        public int TotalCount { get; set; }
        public decimal RevenueAmount { get; set; }
    }

    public class ReceptionistActivityRowViewModel
    {
        public int Id { get; set; }
        public DateTime ActionTime { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = string.Empty;
        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceCode { get; set; }
        public decimal? InvoiceTotalAmount { get; set; }
        public int? PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? Description { get; set; }
    }

    public class ReceptionistDailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal RevenueAmount { get; set; }
        public decimal CumulativeRevenue { get; set; }
    }

    public class ReceptionistPaymentRevenueViewModel
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionCode { get; set; } = string.Empty;

        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        public int? BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}