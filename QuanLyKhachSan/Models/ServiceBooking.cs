using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class ServiceBooking : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }
        [Required]
        [ForeignKey(nameof(Service))]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }
        [Range(1, 1000)]
        public int Quantity { get; set; }
        [Range(0, 999999999)]
        public decimal UnitPrice { get; set; }
        [Range(0, 999999999)]
        public decimal TotalPrice { get; set; }
    }
}
