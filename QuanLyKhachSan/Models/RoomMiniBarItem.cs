using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class RoomMiniBarItem : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Room))]
        public int RoomId { get; set; }
        public Room? Room { get; set; }
        [Required]
        [ForeignKey(nameof(RoomChargeItem))]
        public int RoomChargeItemId { get; set; }
        public RoomChargeItem? RoomChargeItem { get; set; }
        [Range(0, 1000)]
        public int StandardQuantity { get; set; }
        [Range(0, 1000)]
        public int CurrentQuantity { get; set; }
    }
}