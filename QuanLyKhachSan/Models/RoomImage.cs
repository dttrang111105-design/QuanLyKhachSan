using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models
{
    public class RoomImage : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Room))]
        public int RoomId { get; set; }

        public Room Room { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string? Caption { get; set; }

        public bool IsMain { get; set; } = false;
    }
}
