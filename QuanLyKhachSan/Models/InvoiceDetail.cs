using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class InvoiceDetail : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Invoice))]
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
        [Required]
        [StringLength(30)]
        public string DetailType { get; set; }
        [Required]
        [StringLength(200)]
        public string ItemName { get; set; }
        [Range(1, 1000)]
        public int Quantity { get; set; }
        [Range(0, 999999999)]
        public decimal UnitPrice { get; set; }
        [Range(0, 999999999)]
        public decimal Amount { get; set; }
        [StringLength(500)]
        public string? Note { get; set; }
    }
}