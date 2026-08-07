namespace QuanLyKhachSan.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Address { get; set; }
        public string Role { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string? Avatar { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}