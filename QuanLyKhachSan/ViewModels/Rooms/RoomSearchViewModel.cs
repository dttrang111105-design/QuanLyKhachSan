using QuanLyKhachSan.Models;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels.Rooms
{
    public class RoomSearchViewModel
    {
        [Display(Name = "Ngày nhận phòng")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [Display(Name = "Ngày trả phòng")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Display(Name = "Người lớn")]
        [Range(1, 20, ErrorMessage = "Số người lớn phải từ 1 đến 20.")]
        public int Adult { get; set; } = 1;

        [Display(Name = "Trẻ em")]
        [Range(0, 20, ErrorMessage = "Số trẻ em phải từ 0 đến 20.")]
        public int Children { get; set; }

        [Display(Name = "Loại phòng")]
        public int? RoomTypeId { get; set; }

        [Display(Name = "Giá tối thiểu")]
        [Range(0, 100000000, ErrorMessage = "Giá tối thiểu không hợp lệ.")]
        public decimal? MinPrice { get; set; }

        [Display(Name = "Giá tối đa")]
        [Range(0, 100000000, ErrorMessage = "Giá tối đa không hợp lệ.")]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Từ khóa")]
        [StringLength(100)]
        public string? Keyword { get; set; }

        public bool HasSearched { get; set; }

        public List<Room> Rooms { get; set; } = new();

        public List<RoomType> RoomTypes { get; set; } = new();
    }
}