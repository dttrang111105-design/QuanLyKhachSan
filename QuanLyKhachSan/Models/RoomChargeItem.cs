using System.ComponentModel.DataAnnotations;
namespace QuanLyKhachSan.Models
{
    public class RoomChargeItem : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; }
        [Required]
        [StringLength(30)]
        public string Category { get; set; }
        [Range(0, 999999999)]
        public decimal UsedPrice { get; set; }
        [Range(0, 999999999)]
        public decimal DamagedPrice { get; set; }
        [Range(0, 999999999)]
        public decimal LostPrice { get; set; }
    }
}