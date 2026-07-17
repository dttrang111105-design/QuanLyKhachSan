using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class Service : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; }

        [Required]
        [Range(0, 999999999)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation

        public ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();
    }
}
