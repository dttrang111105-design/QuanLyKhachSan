using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
namespace QuanLyKhachSan.ViewModels
{
    public class CustomerBookServiceViewModel
    {
        public int ServiceId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn booking/phòng.")]
        [Range(1, int.MaxValue, ErrorMessage = "Booking không hợp lệ.")]
        public int BookingId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
        public int Quantity { get; set; } = 1;
        [ValidateNever]
        public string ServiceName { get; set; } = string.Empty;
        [ValidateNever]
        public string? Description { get; set; }
        [ValidateNever]
        public decimal Price { get; set; }
        // Danh sách booking đang CheckedIn
        [ValidateNever]
        public List<SelectListItem> ActiveBookings { get; set; } = new();
    }
}