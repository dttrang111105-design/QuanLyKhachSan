using System.ComponentModel.DataAnnotations;
namespace QuanLyKhachSan.ViewModels
{
    public class RoomMiniBarViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public List<RoomMiniBarItemViewModel> Items { get; set; } = new List<RoomMiniBarItemViewModel>();
    }
    public class RoomMiniBarItemViewModel
    {
        public int? Id { get; set; }
        public int RoomChargeItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UsedPrice { get; set; }
        public bool IsEnabled { get; set; }
        [Range(0, 1000, ErrorMessage = "Số lượng chuẩn phải từ 0 đến 1000.")]
        public int StandardQuantity { get; set; }
        [Range(0, 1000, ErrorMessage = "Số lượng hiện có phải từ 0 đến 1000.")]
        public int CurrentQuantity { get; set; }
    }
}