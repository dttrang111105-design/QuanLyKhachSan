using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels
{
    public class BookingCreateViewModel
    {
        public int RoomId { get; set; }

        public string RoomNumber { get; set; } = "";

        public decimal PricePerNight { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 20, ErrorMessage = "Số người lớn phải từ 1 đến 20.")]
        public int Adult { get; set; } = 1;

        [Range(0, 20)]
        public int Children { get; set; }

        [Range(0, 999999999)]
        public decimal Deposit { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}