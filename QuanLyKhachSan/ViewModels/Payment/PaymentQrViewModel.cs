using QuanLyKhachSan.Enums;
namespace QuanLyKhachSan.ViewModels.Payment
{
    public class PaymentQrViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; }
        public decimal Amount { get; set; }
        public string BankCode { get; set; }
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public string QrUrl { get; set; }
    }
}