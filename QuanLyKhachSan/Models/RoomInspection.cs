using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class RoomInspection : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Booking))]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }
        public DateTime InspectionDate { get; set; } = DateTime.Now;
        [Range(0, 999999999)]
        public decimal TotalCharge { get; set; }
        [StringLength(500)]
        public string? Note { get; set; }
        // Navigation
        public ICollection<RoomInspectionDetail> RoomInspectionDetails { get; set; } = new List<RoomInspectionDetail>();
    }
}