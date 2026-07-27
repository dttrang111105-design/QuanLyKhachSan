using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels
{
    public class StaffBookServiceViewModel
    {
        [Display(Name = "Booking đang nhận phòng")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn booking.")]
        public int BookingId { get; set; }

        [Display(Name = "Dịch vụ")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
        public int ServiceId { get; set; }

        [Display(Name = "Số lượng")]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
        public int Quantity { get; set; } = 1;

        public List<SelectListItem> ActiveBookings { get; set; } = new();
        public List<SelectListItem> AvailableServices { get; set; } = new();
    }
}