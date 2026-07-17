using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels.Profile
{
    public class EditProfileViewModel
    {
        [Required]
        public string FullName { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";

        [Phone]
        public string Phone { get; set; } = "";

        public string? Address { get; set; }

        public string? Avatar { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }
}