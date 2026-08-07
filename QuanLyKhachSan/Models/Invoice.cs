using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class Invoice : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }
        [Required]
        [StringLength(30)]
        public string InvoiceCode { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        [Range(0, 999999999)]
        public decimal RoomAmount { get; set; }
        [Range(0, 999999999)]
        public decimal ServiceAmount { get; set; }
        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }
        [Range(0, 100)]
        public decimal TaxPercent { get; set; }
        [Range(0, 999999999)]
        public decimal TotalAmount { get; set; }
        // Navigation
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
