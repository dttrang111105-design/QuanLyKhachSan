using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class BookingDetail : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }
        [Required]
        [ForeignKey(nameof(Room))]
        public int RoomId { get; set; }
        public Room? Room { get; set; }
        public decimal PricePerNight { get; set; }
        public int NumberOfNights { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
