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
                        var roomDetails = invoice.InvoiceDetails
                            .Where(d => !d.IsDeleted && d.DetailType == "Room")
                            .ToList();
                        if (roomDetails.Any())
                        {
                            foreach (var detail in roomDetails)
                            {
                                col.Item().Text(
                                    $"{detail.ItemName}   |   " +
                                    $"{detail.Quantity} đêm   |   " +
                                    $"{detail.UnitPrice:N0} VNĐ   |   " +
                                    $"{detail.Amount:N0} VNĐ");
                            }
                        }
                        else
                        {
                            foreach (var room in invoice.Booking.BookingDetails)
                            {
                                col.Item().Text(
                                    $"{room.Room.RoomNumber}   |   " +
                                    $"{room.NumberOfNights} đêm   |   " +
                                    $"{room.TotalPrice:N0} VNĐ");
                            }
                        }
                        col.Item().LineHorizontal(1);
                        col.Item().Text("Dịch vụ").Bold();
                        var serviceDetails = invoice.InvoiceDetails
                            .Where(d => !d.IsDeleted && d.DetailType == "Service")
                            .ToList();
                        if (serviceDetails.Any())
                        {
                            foreach (var detail in serviceDetails)
                            {
                                col.Item().Text(
                                    $"{detail.ItemName}   x{detail.Quantity}   " +
                                    $"{detail.UnitPrice:N0} VNĐ   |   " +
                                    $"{detail.Amount:N0} VNĐ");
                            }
                        }
                        else
                        {
                            foreach (var service in invoice.Booking.ServiceBookings)
                            {
                                col.Item().Text(
                                    $"{service.Service.ServiceName}   x{service.Quantity}   " +
                                    $"{service.TotalPrice:N0} VNĐ");
                            }
                        }
                        var miniBarDetails = invoice.InvoiceDetails
                            .Where(d => !d.IsDeleted && d.DetailType == "MiniBar")
                            .ToList();
                        var compensationDetails = invoice.InvoiceDetails
                            .Where(d => !d.IsDeleted && d.DetailType == "Compensation")
                            .ToList();
                        if (miniBarDetails.Any() || compensationDetails.Any())
                        {
                            col.Item().LineHorizontal(1);
                            col.Item().Text("Phụ thu kiểm tra phòng").Bold();
                            foreach (var detail in miniBarDetails)
                            {
                                col.Item().Text(
                                    $"MiniBar - {detail.ItemName}   x{detail.Quantity}   " +
                                    $"{detail.UnitPrice:N0} VNĐ   |   " +
                                    $"{detail.Amount:N0} VNĐ");
                            }
                            foreach (var detail in compensationDetails)
                            {
                                col.Item().Text(
                                    $"Đền bù - {detail.ItemName}   x{detail.Quantity}   " +
                                    $"{detail.UnitPrice:N0} VNĐ   |   " +
                                    $"{detail.Amount:N0} VNĐ");
                            }
                        }
                        decimal miniBarAmount = miniBarDetails.Sum(d => d.Amount);
                        decimal compensationAmount = compensationDetails.Sum(d => d.Amount);
                        col.Item().LineHorizontal(1);
                        col.Item().Text($"Tiền phòng: {invoice.RoomAmount:N0} VNĐ");
                        col.Item().Text($"Tiền dịch vụ: {invoice.ServiceAmount:N0} VNĐ");
                        if (miniBarAmount > 0)
                        {
                            col.Item().Text($"MiniBar: {miniBarAmount:N0} VNĐ");
                        }
                        if (compensationAmount > 0)
                        {
                            col.Item().Text($"Đền bù tài sản: {compensationAmount:N0} VNĐ");
                        }
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