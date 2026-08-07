using System.ComponentModel.DataAnnotations;
namespace QuanLyKhachSan.ViewModels
{
    public class RoomInspectionViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        [Display(Name = "Ghi chú kiểm tra")]
        public string? Note { get; set; }
        public List<RoomInspectionRoomViewModel> Rooms { get; set; } = new List<RoomInspectionRoomViewModel>();
    }
    public class RoomInspectionRoomViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public List<RoomInspectionItemViewModel> Items { get; set; } = new List<RoomInspectionItemViewModel>();
    }
    public class RoomInspectionItemViewModel
    {
        public int RoomChargeItemId { get; set; }
        public int? RoomMiniBarItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal UsedPrice { get; set; }
        public decimal DamagedPrice { get; set; }
        public decimal LostPrice { get; set; }
        public int AvailableQuantity { get; set; }
        [Range(0, 1000, ErrorMessage = "Số lượng phải từ 0 đến 1000.")]
        public int Quantity { get; set; }
        [StringLength(30)]
        public string ResultType { get; set; } = "Normal";
    }
}