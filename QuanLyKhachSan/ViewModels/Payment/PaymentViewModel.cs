using QuanLyKhachSan.Enums;
using System.ComponentModel.DataAnnotations;
namespace QuanLyKhachSan.ViewModels.Payment
{
    public class PaymentViewModel
    {
        public int InvoiceId { get; set; }
        [Display(Name = "Số tiền")]
        public decimal Amount { get; set; }
        [Required]
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; }
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}