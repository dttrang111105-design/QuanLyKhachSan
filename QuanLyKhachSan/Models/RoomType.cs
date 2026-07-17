using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class RoomType : BaseEntity
    {
        [Required(ErrorMessage = "Tên loại phòng không được để trống")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(0, 100000000)]
        public decimal BasePrice { get; set; }

        [Range(1, 20)]
        public int MaxOccupancy { get; set; }

        [StringLength(50)]
        public string BedType { get; set; }

        [Range(1, 500)]
        public double Area { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
