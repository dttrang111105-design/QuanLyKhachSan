using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuanLyKhachSan.Models;

namespace QuanLyKhachSan.Services
{
    public class InvoicePdfService
    {
        public byte[] Generate(Invoice invoice)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("HÓA ĐƠN THANH TOÁN")
                        .FontSize(24)
                        .Bold()
                        .AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Mã hóa đơn: {invoice.InvoiceCode}");
                        col.Item().Text($"Ngày lập: {invoice.InvoiceDate:dd/MM/yyyy HH:mm}");

                        col.Item().Text($"Khách hàng: {invoice.Booking.Customer.FullName}");

                        col.Item().Text($"Booking: {invoice.Booking.BookingCode}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text("Danh sách phòng").Bold();

                        foreach (var room in invoice.Booking.BookingDetails)
                        {
                            col.Item().Text(

                                $"{room.Room.RoomNumber}   |   " +

                                $"{room.NumberOfNights} đêm   |   " +

                                $"{room.TotalPrice:N0} VNĐ");
                        }

                        col.Item().LineHorizontal(1);

                        col.Item().Text("Dịch vụ").Bold();

                        foreach (var service in invoice.Booking.ServiceBookings)
                        {
                            col.Item().Text(

                                $"{service.Service.ServiceName}   x{service.Quantity}   " +

                                $"{service.TotalPrice:N0} VNĐ");
                        }

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Tiền phòng: {invoice.RoomAmount:N0}");

                        col.Item().Text($"Tiền dịch vụ: {invoice.ServiceAmount:N0}");

                        col.Item().Text($"Giảm giá: {invoice.DiscountPercent}%");

                        col.Item().Text($"Thuế: {invoice.TaxPercent}%");

                        col.Item()

                            .Text($"TỔNG CỘNG: {invoice.TotalAmount:N0} VNĐ")

                            .FontSize(18)

                            .Bold();
                    });

                    page.Footer()

                        .AlignCenter()

                        .Text("Luxury Hotel");
                });

            }).GeneratePdf();
        }
    }
}