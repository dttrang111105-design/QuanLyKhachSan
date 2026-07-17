using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models
{
    public class Employee : BaseEntity
    {
        [ForeignKey(nameof(Account))]
        public int AccountId { get; set; }

        public Account? Account { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(10)]
        public string Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Phone]
        [StringLength(15)]
        public string Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string Position { get; set; }

        public decimal Salary { get; set; }

        public DateTime HireDate { get; set; }

        public bool Status { get; set; } = true;

        public string? Avatar { get; set; }
    }
}
