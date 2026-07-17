using QuanLyKhachSan.Enums;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class Account : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Phone]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }

        // Navigation
        public Customer? Customer { get; set; } = null!;

        public Employee? Employee { get; set; }
    }
}
