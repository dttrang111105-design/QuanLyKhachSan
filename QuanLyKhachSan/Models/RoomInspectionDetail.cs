using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyKhachSan.Models
{
    public class RoomInspectionDetail : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(RoomInspection))]
        public int RoomInspectionId { get; set; }
        public RoomInspection? RoomInspection { get; set; }
        [Required]
        [ForeignKey(nameof(Room))]
        public int RoomId { get; set; }
        public Room? Room { get; set; }
        [Required]
        [ForeignKey(nameof(RoomChargeItem))]
        public int RoomChargeItemId { get; set; }
        public RoomChargeItem? RoomChargeItem { get; set; }
        [Required]
        [StringLength(150)]
        public string ItemName { get; set; }
        [Required]
        [StringLength(30)]
        public string Category { get; set; }
        [Required]
        [StringLength(30)]
        public string ResultType { get; set; }
        [Range(0, 1000)]
        public int Quantity { get; set; }
        [Range(0, 999999999)]
        public decimal UnitPrice { get; set; }
        [Range(0, 999999999)]
        public decimal Amount { get; set; }
        [StringLength(500)]
        public string? Note { get; set; }
    }
}