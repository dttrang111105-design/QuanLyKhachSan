using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.ViewModels
{
    public class CreateServiceBookingViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ")]
        public int ServiceId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; }

        public List<SelectListItem> Services { get; set; } = new();
    }
}