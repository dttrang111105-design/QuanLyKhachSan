using QuanLyKhachSan.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class Payment : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Invoice))]
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        [Range(0, 999999999)]
        public decimal Amount { get; set; }
        [StringLength(100)]
        public string? TransactionCode { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        [StringLength(500)]
        public string? Note { get; set; }
    }
}
