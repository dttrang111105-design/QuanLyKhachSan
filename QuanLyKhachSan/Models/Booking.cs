using QuanLyKhachSan.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan.Models
{
    public class Booking : BaseEntity
    {
        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        [Required]
        [StringLength(20)]
        public string BookingCode { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 20)]
        public int Adult { get; set; }

        [Range(0, 20)]
        public int Children { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Range(0, 999999999)]
        public decimal Deposit { get; set; }

        [Range(0, 999999999)]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        //---------------- Navigation ----------------

        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

        public ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();

        public Invoice? Invoice { get; set; }
    }
}
