using System.ComponentModel.DataAnnotations;
namespace QuanLyKhachSan.ViewModels
{
    public class BookingCreateViewModel
    {
        [Display(Name = "Khách trực tiếp đến quầy")]
        public bool CreateNewCustomer { get; set; }
        [Display(Name = "Khách hàng có sẵn")]
        public int? CustomerId { get; set; }
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ và tên khách hàng")]
        public string? GuestFullName { get; set; }
        [StringLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
        [Display(Name = "Giới tính")]
        public string? GuestGender { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? GuestDateOfBirth { get; set; }
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string? GuestPhone { get; set; }
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [Display(Name = "Email")]
        public string? GuestEmail { get; set; }
        [StringLength(200, ErrorMessage = "Địa chỉ tối đa 200 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string? GuestAddress { get; set; }
        [StringLength(20, ErrorMessage = "CCCD tối đa 20 ký tự.")]
        [Display(Name = "CCCD")]
        public string? GuestCitizenId { get; set; }
        public int RoomId { get; set; }
        [Display(Name = "Số phòng")]
        public string RoomNumber { get; set; } = string.Empty;
        [Display(Name = "Loại phòng")]
        public string RoomTypeName { get; set; } = string.Empty;
        [Display(Name = "Giá mỗi đêm")]
        public decimal PricePerNight { get; set; }
        [Display(Name = "Sức chứa tối đa")]
        public int MaxOccupancy { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; } = DateTime.Today;
        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);
        [Range(1, 20, ErrorMessage = "Số người lớn phải từ 1 đến 20.")]
        [Display(Name = "Người lớn")]
        public int Adult { get; set; } = 1;
        [Range(0, 20, ErrorMessage = "Số trẻ em phải từ 0 đến 20.")]
        [Display(Name = "Trẻ em")]
        public int Children { get; set; }
        [Range(0, 999999999, ErrorMessage = "Tiền cọc không được âm.")]
        [Display(Name = "Tiền cọc")]
        public decimal Deposit { get; set; }
        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
        public int NumberOfNights => Math.Max(0, (CheckOutDate.Date - CheckInDate.Date).Days);
        public int TotalGuests => Adult + Children;
        public decimal EstimatedTotal => NumberOfNights * PricePerNight;
    }
}