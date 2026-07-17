using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Address { get; set; }

        public string CitizenId { get; set; }

    }
}