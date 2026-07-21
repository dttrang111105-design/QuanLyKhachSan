using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels.Reviews
{
    public class ReviewFormViewModel
    {
        public int? Id { get; set; }

        public int BookingId { get; set; }

        public int RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;

        public string? RoomTypeName { get; set; }

        public string? BookingCode { get; set; }

        [Display(Name = "Mức đánh giá")]
        [Range(1, 5, ErrorMessage = "Mức đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; } = 5;

        [Display(Name = "Nhận xét")]
        [StringLength(1000, ErrorMessage = "Nhận xét không được vượt quá 1000 ký tự.")]
        public string? Comment { get; set; }
    }
}