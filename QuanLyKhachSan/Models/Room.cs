using QuanLyKhachSan.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models
{
    public class Room : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(RoomType))]
        public int RoomTypeId { get; set; }

        public RoomType? RoomType { get; set; }

        [Required]
        [StringLength(10)]
        public string RoomNumber { get; set; }

        [Range(1, 100)]
        public int Floor { get; set; }

        public RoomStatus Status { get; set; } = RoomStatus.Available;

        [Range(0, 100000000)]
        public decimal PriceDay { get; set; }

        [Range(0, 100000000)]
        public decimal PriceWeek { get; set; }

        [Range(1, 500)]
        public double RoomSize { get; set; }

        [Range(1, 20)]
        public int MaxOccupancy { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation
        public ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();

        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
