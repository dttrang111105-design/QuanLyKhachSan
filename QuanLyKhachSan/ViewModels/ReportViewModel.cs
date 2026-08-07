namespace QuanLyKhachSan.ViewModels
{
    public class ReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string GroupBy { get; set; } = "day";
        public string PeriodText { get; set; } = string.Empty;

        public decimal TotalRevenue { get; set; }
        public int PaidTransactionCount { get; set; }
        public int BookingCount { get; set; }
        public int CheckedOutBookingCount { get; set; }
        public int CancelledBookingCount { get; set; }
        public int InvoiceCount { get; set; }
        public decimal InvoiceValue { get; set; }
        public decimal TotalDeposit { get; set; }

        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public int OccupiedRoomNights { get; set; }
        public decimal OccupancyRate { get; set; }

        public List<ReportRevenueRowViewModel> RevenueRows { get; set; }
            = new List<ReportRevenueRowViewModel>();

        public List<ReportInvoiceCategoryViewModel> InvoiceCategories { get; set; }
            = new List<ReportInvoiceCategoryViewModel>();

        public List<ReportBookingStatusViewModel> BookingStatuses { get; set; }
            = new List<ReportBookingStatusViewModel>();

        public List<ReportBookingDailyViewModel> BookingDailyRows { get; set; }
            = new List<ReportBookingDailyViewModel>();

        public List<ReportRoomViewModel> RoomRows { get; set; }
            = new List<ReportRoomViewModel>();

        public List<ReportServiceViewModel> ServiceRows { get; set; }
            = new List<ReportServiceViewModel>();

        public List<ReportPaymentMethodViewModel> PaymentMethods { get; set; }
            = new List<ReportPaymentMethodViewModel>();

        public List<ReportPaymentDetailViewModel> PaymentDetails { get; set; }
            = new List<ReportPaymentDetailViewModel>();
    }

    public class ReportRevenueRowViewModel
    {
        public DateTime PeriodStart { get; set; }
        public string Label { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal CumulativeRevenue { get; set; }
    }

    public class ReportInvoiceCategoryViewModel
    {
        public string Category { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ReportBookingStatusViewModel
    {
        public string Status { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ReportBookingDailyViewModel
    {
        public DateTime Date { get; set; }
        public int NewBookings { get; set; }
        public int CheckIns { get; set; }
        public int CheckOuts { get; set; }
        public int CancelledBookings { get; set; }
    }

    public class ReportRoomViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public int StayCount { get; set; }
        public int OccupiedNights { get; set; }
        public decimal RoomValue { get; set; }
    }

    public class ReportServiceViewModel
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReportPaymentMethodViewModel
    {
        public string Method { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReportPaymentDetailViewModel
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string InvoiceCode { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}