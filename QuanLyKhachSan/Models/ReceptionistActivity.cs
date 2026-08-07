using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models
{
    public class ReceptionistActivity : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [ForeignKey(nameof(Booking))]
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        [ForeignKey(nameof(Invoice))]
        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        [ForeignKey(nameof(Payment))]
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}