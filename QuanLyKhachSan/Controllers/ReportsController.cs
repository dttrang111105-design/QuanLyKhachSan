using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels;

namespace QuanLyKhachSan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            string? groupBy,
            string? preset)
        {
            var range = ResolveRange(fromDate, toDate, preset);
            string normalizedGroupBy = NormalizeGroupBy(groupBy);

            if (range.FromDate > range.ToDate)
            {
                TempData["Error"] = "Từ ngày không được lớn hơn đến ngày.";
                range = ResolveRange(null, null, "month");
            }

            var model = await BuildReportAsync(
                range.FromDate,
                range.ToDate,
                normalizedGroupBy);

            return View(model);
        }

        public async Task<IActionResult> ExportExcel(
            DateTime? fromDate,
            DateTime? toDate,
            string? groupBy)
        {
            var range = ResolveRange(fromDate, toDate, null);
            string normalizedGroupBy = NormalizeGroupBy(groupBy);

            if (range.FromDate > range.ToDate)
            {
                TempData["Error"] = "Từ ngày không được lớn hơn đến ngày.";
                return RedirectToAction(nameof(Index));
            }

            var model = await BuildReportAsync(
                range.FromDate,
                range.ToDate,
                normalizedGroupBy);

            using var workbook = new XLWorkbook();

            CreateOverviewSheet(workbook, model);
            CreateRevenueSheet(workbook, model);
            CreateInvoiceSheet(workbook, model);
            CreateBookingSheet(workbook, model);
            CreateRoomSheet(workbook, model);
            CreateServiceSheet(workbook, model);
            CreatePaymentSheet(workbook, model);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            string fileName =
                $"BaoCaoKhachSan_{model.FromDate:yyyyMMdd}_{model.ToDate:yyyyMMdd}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private async Task<ReportViewModel> BuildReportAsync(
            DateTime fromDate,
            DateTime toDate,
            string groupBy)
        {
            DateTime from = fromDate.Date;
            DateTime to = toDate.Date;
            DateTime toExclusive = to.AddDays(1);

            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                        .ThenInclude(b => b.Customer)
                .Where(p =>
                    !p.IsDeleted &&
                    p.PaymentStatus == PaymentStatus.Paid &&
                    p.PaymentDate >= from &&
                    p.PaymentDate < toExclusive)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            var invoices = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Booking)
                .Include(i => i.InvoiceDetails.Where(d => !d.IsDeleted))
                .Where(i =>
                    !i.IsDeleted &&
                    i.InvoiceDate >= from &&
                    i.InvoiceDate < toExclusive)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

            var bookingsCreated = await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    !b.IsDeleted &&
                    b.BookingDate >= from &&
                    b.BookingDate < toExclusive)
                .ToListAsync();

            var bookingMovements = await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    !b.IsDeleted &&
                    (b.BookingDate >= from && b.BookingDate < toExclusive ||
                     b.CheckInDate >= from && b.CheckInDate < toExclusive ||
                     b.CheckOutDate >= from && b.CheckOutDate < toExclusive))
                .ToListAsync();

            var rooms = await _context.Rooms
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .ToListAsync();

            var roomDetails = await _context.BookingDetails
                .AsNoTracking()
                .Include(d => d.Booking)
                .Include(d => d.Room)
                    .ThenInclude(r => r.RoomType)
                .Where(d =>
                    !d.IsDeleted &&
                    d.Booking != null &&
                    !d.Booking.IsDeleted &&
                    (d.Booking.Status == BookingStatus.CheckedIn ||
                     d.Booking.Status == BookingStatus.CheckedOut) &&
                    d.Booking.CheckInDate < toExclusive &&
                    d.Booking.CheckOutDate > from)
                .ToListAsync();

            var serviceBookings = await _context.ServiceBookings
                .AsNoTracking()
                .Include(s => s.Service)
                .Include(s => s.Booking)
                .Where(s =>
                    !s.IsDeleted &&
                    s.CreatedAt >= from &&
                    s.CreatedAt < toExclusive &&
                    s.Booking != null &&
                    !s.Booking.IsDeleted &&
                    s.Booking.Status != BookingStatus.Cancelled)
                .ToListAsync();

            var revenueRows = BuildRevenueRows(payments, groupBy);
            var invoiceCategories = BuildInvoiceCategories(invoices);
            var bookingStatuses = BuildBookingStatuses(bookingsCreated);
            var bookingDailyRows = BuildBookingDailyRows(
                bookingMovements,
                from,
                to);
            var roomRows = BuildRoomRows(roomDetails, from, toExclusive);
            var serviceRows = BuildServiceRows(serviceBookings);
            var paymentMethods = BuildPaymentMethods(payments);
            var paymentDetails = BuildPaymentDetails(payments);

            int totalRooms = rooms.Count;
            int availableRooms = rooms.Count(r => r.Status == RoomStatus.Available);
            int occupiedRooms = rooms.Count(r => r.Status == RoomStatus.Occupied);
            int maintenanceRooms = rooms.Count(r => r.Status == RoomStatus.Maintenance);
            int occupiedRoomNights = roomRows.Sum(r => r.OccupiedNights);

            int reportDays = Math.Max(1, (toExclusive - from).Days);
            int roomsForCapacity = Math.Max(0, totalRooms - maintenanceRooms);
            decimal occupancyRate = 0;

            if (roomsForCapacity > 0)
            {
                decimal capacityNights = roomsForCapacity * reportDays;
                occupancyRate = occupiedRoomNights / capacityNights * 100m;

                if (occupancyRate > 100m)
                {
                    occupancyRate = 100m;
                }
            }

            return new ReportViewModel
            {
                FromDate = from,
                ToDate = to,
                GroupBy = groupBy,
                PeriodText = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}",

                TotalRevenue = payments.Sum(p => p.Amount),
                PaidTransactionCount = payments.Count,
                BookingCount = bookingsCreated.Count,
                CheckedOutBookingCount = bookingsCreated.Count(
                    b => b.Status == BookingStatus.CheckedOut),
                CancelledBookingCount = bookingsCreated.Count(
                    b => b.Status == BookingStatus.Cancelled),
                InvoiceCount = invoices.Count,
                InvoiceValue = invoices.Sum(i => i.TotalAmount),
                TotalDeposit = bookingsCreated.Sum(b => b.Deposit),

                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                OccupiedRooms = occupiedRooms,
                MaintenanceRooms = maintenanceRooms,
                OccupiedRoomNights = occupiedRoomNights,
                OccupancyRate = occupancyRate,

                RevenueRows = revenueRows,
                InvoiceCategories = invoiceCategories,
                BookingStatuses = bookingStatuses,
                BookingDailyRows = bookingDailyRows,
                RoomRows = roomRows,
                ServiceRows = serviceRows,
                PaymentMethods = paymentMethods,
                PaymentDetails = paymentDetails
            };
        }

        private static List<ReportRevenueRowViewModel> BuildRevenueRows(
            List<Payment> payments,
            string groupBy)
        {
            var grouped = payments
                .GroupBy(p => GetPeriodStart(p.PaymentDate, groupBy))
                .OrderBy(g => g.Key)
                .ToList();

            decimal cumulative = 0;
            var result = new List<ReportRevenueRowViewModel>();

            foreach (var group in grouped)
            {
                decimal revenue = group.Sum(p => p.Amount);
                cumulative += revenue;

                result.Add(new ReportRevenueRowViewModel
                {
                    PeriodStart = group.Key,
                    Label = GetPeriodLabel(group.Key, groupBy),
                    TransactionCount = group.Count(),
                    Revenue = revenue,
                    CumulativeRevenue = cumulative
                });
            }

            return result;
        }

        private static List<ReportInvoiceCategoryViewModel> BuildInvoiceCategories(
            List<Invoice> invoices)
        {
            decimal roomAmount = 0;
            decimal serviceAmount = 0;
            decimal miniBarAmount = 0;
            decimal compensationAmount = 0;

            foreach (var invoice in invoices)
            {
                var details = invoice.InvoiceDetails
                    .Where(d => !d.IsDeleted)
                    .ToList();

                var roomDetails = details
                    .Where(d => d.DetailType == "Room")
                    .ToList();
                var serviceDetails = details
                    .Where(d => d.DetailType == "Service")
                    .ToList();

                roomAmount += roomDetails.Any()
                    ? roomDetails.Sum(d => d.Amount)
                    : invoice.RoomAmount;

                serviceAmount += serviceDetails.Any()
                    ? serviceDetails.Sum(d => d.Amount)
                    : invoice.ServiceAmount;

                miniBarAmount += details
                    .Where(d => d.DetailType == "MiniBar")
                    .Sum(d => d.Amount);

                compensationAmount += details
                    .Where(d => d.DetailType == "Compensation")
                    .Sum(d => d.Amount);
            }

            return new List<ReportInvoiceCategoryViewModel>
            {
                new ReportInvoiceCategoryViewModel
                {
                    Category = "Room",
                    Label = "Tiền phòng",
                    Amount = roomAmount
                },
                new ReportInvoiceCategoryViewModel
                {
                    Category = "Service",
                    Label = "Dịch vụ",
                    Amount = serviceAmount
                },
                new ReportInvoiceCategoryViewModel
                {
                    Category = "MiniBar",
                    Label = "Minibar",
                    Amount = miniBarAmount
                },
                new ReportInvoiceCategoryViewModel
                {
                    Category = "Compensation",
                    Label = "Bồi thường",
                    Amount = compensationAmount
                }
            };
        }

        private static List<ReportBookingStatusViewModel> BuildBookingStatuses(
            List<Booking> bookings)
        {
            return new List<ReportBookingStatusViewModel>
            {
                CreateBookingStatus("Pending", "Đang chờ", bookings, BookingStatus.Pending),
                CreateBookingStatus("Confirmed", "Đã xác nhận", bookings, BookingStatus.Confirmed),
                CreateBookingStatus("CheckedIn", "Đang lưu trú", bookings, BookingStatus.CheckedIn),
                CreateBookingStatus("CheckedOut", "Đã trả phòng", bookings, BookingStatus.CheckedOut),
                CreateBookingStatus("Cancelled", "Đã hủy", bookings, BookingStatus.Cancelled)
            };
        }

        private static ReportBookingStatusViewModel CreateBookingStatus(
            string status,
            string label,
            List<Booking> bookings,
            BookingStatus bookingStatus)
        {
            return new ReportBookingStatusViewModel
            {
                Status = status,
                Label = label,
                Count = bookings.Count(b => b.Status == bookingStatus)
            };
        }

        private static List<ReportBookingDailyViewModel> BuildBookingDailyRows(
            List<Booking> bookings,
            DateTime from,
            DateTime to)
        {
            var rows = new List<ReportBookingDailyViewModel>();

            for (DateTime date = from; date <= to; date = date.AddDays(1))
            {
                DateTime next = date.AddDays(1);

                int newBookings = bookings.Count(b =>
                    b.BookingDate >= date && b.BookingDate < next);

                int checkIns = bookings.Count(b =>
                    (b.Status == BookingStatus.CheckedIn ||
                     b.Status == BookingStatus.CheckedOut) &&
                    b.CheckInDate >= date &&
                    b.CheckInDate < next);

                int checkOuts = bookings.Count(b =>
                    b.Status == BookingStatus.CheckedOut &&
                    b.CheckOutDate >= date &&
                    b.CheckOutDate < next);

                // Project hiện chưa có CancelledAt, vì vậy cột Hủy được quy theo
                // ngày tạo Booking để vẫn có thể thống kê nhất quán từ dữ liệu hiện có.
                int cancelled = bookings.Count(b =>
                    b.Status == BookingStatus.Cancelled &&
                    b.BookingDate >= date &&
                    b.BookingDate < next);

                if (newBookings > 0 || checkIns > 0 || checkOuts > 0 || cancelled > 0)
                {
                    rows.Add(new ReportBookingDailyViewModel
                    {
                        Date = date,
                        NewBookings = newBookings,
                        CheckIns = checkIns,
                        CheckOuts = checkOuts,
                        CancelledBookings = cancelled
                    });
                }
            }

            return rows;
        }

        private static List<ReportRoomViewModel> BuildRoomRows(
            List<BookingDetail> details,
            DateTime from,
            DateTime toExclusive)
        {
            var workingRows = details
                .Select(detail =>
                {
                    DateTime checkIn = detail.Booking!.CheckInDate.Date;
                    DateTime checkOut = detail.Booking.CheckOutDate.Date;

                    DateTime overlapStart = checkIn > from ? checkIn : from;
                    DateTime overlapEnd = checkOut < toExclusive
                        ? checkOut
                        : toExclusive;

                    int occupiedNights = Math.Max(
                        0,
                        (overlapEnd - overlapStart).Days);

                    decimal valuePerNight = detail.NumberOfNights > 0
                        ? detail.TotalPrice / detail.NumberOfNights
                        : detail.PricePerNight;

                    return new
                    {
                        Detail = detail,
                        OccupiedNights = occupiedNights,
                        RoomValue = valuePerNight * occupiedNights
                    };
                })
                .Where(x => x.OccupiedNights > 0)
                .ToList();

            return workingRows
                .GroupBy(x => new
                {
                    x.Detail.RoomId,
                    RoomNumber = x.Detail.Room?.RoomNumber ?? x.Detail.RoomId.ToString(),
                    RoomTypeName = x.Detail.Room?.RoomType?.Name ?? "Chưa xác định"
                })
                .Select(group => new ReportRoomViewModel
                {
                    RoomId = group.Key.RoomId,
                    RoomNumber = group.Key.RoomNumber,
                    RoomTypeName = group.Key.RoomTypeName,
                    StayCount = group
                        .Select(x => x.Detail.BookingId)
                        .Distinct()
                        .Count(),
                    OccupiedNights = group.Sum(x => x.OccupiedNights),
                    RoomValue = group.Sum(x => x.RoomValue)
                })
                .OrderByDescending(x => x.OccupiedNights)
                .ThenByDescending(x => x.StayCount)
                .ThenBy(x => x.RoomNumber)
                .ToList();
        }

        private static List<ReportServiceViewModel> BuildServiceRows(
            List<ServiceBooking> serviceBookings)
        {
            return serviceBookings
                .GroupBy(s => new
                {
                    s.ServiceId,
                    ServiceName = s.Service?.ServiceName ?? "Dịch vụ",
                    Category = s.Service?.Category ?? "Khác"
                })
                .Select(group => new ReportServiceViewModel
                {
                    ServiceId = group.Key.ServiceId,
                    ServiceName = group.Key.ServiceName,
                    Category = group.Key.Category,
                    UsageCount = group.Count(),
                    Quantity = group.Sum(x => x.Quantity),
                    Amount = group.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenByDescending(x => x.Amount)
                .ThenBy(x => x.ServiceName)
                .ToList();
        }

        private static List<ReportPaymentMethodViewModel> BuildPaymentMethods(
            List<Payment> payments)
        {
            return payments
                .GroupBy(p => p.PaymentMethod)
                .Select(group => new ReportPaymentMethodViewModel
                {
                    Method = group.Key.ToString(),
                    Label = GetPaymentMethodLabel(group.Key),
                    TransactionCount = group.Count(),
                    Amount = group.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();
        }

        private static List<ReportPaymentDetailViewModel> BuildPaymentDetails(
            List<Payment> payments)
        {
            return payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new ReportPaymentDetailViewModel
                {
                    PaymentId = p.Id,
                    PaymentDate = p.PaymentDate,
                    TransactionCode = p.TransactionCode ?? string.Empty,
                    InvoiceCode = p.Invoice?.InvoiceCode ?? string.Empty,
                    BookingCode = p.Invoice?.Booking?.BookingCode ?? string.Empty,
                    CustomerName = p.Invoice?.Booking?.Customer?.FullName ?? "Khách hàng",
                    PaymentMethod = GetPaymentMethodLabel(p.PaymentMethod),
                    Amount = p.Amount
                })
                .ToList();
        }

        private static DateTime GetPeriodStart(DateTime date, string groupBy)
        {
            return groupBy switch
            {
                "month" => new DateTime(date.Year, date.Month, 1),
                "year" => new DateTime(date.Year, 1, 1),
                _ => date.Date
            };
        }

        private static string GetPeriodLabel(DateTime date, string groupBy)
        {
            return groupBy switch
            {
                "month" => date.ToString("MM/yyyy"),
                "year" => date.ToString("yyyy"),
                _ => date.ToString("dd/MM/yyyy")
            };
        }

        private static string GetPaymentMethodLabel(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Tiền mặt",
                PaymentMethod.BankTransfer => "Chuyển khoản",
                PaymentMethod.VNPay => "VNPay",
                PaymentMethod.Momo => "MoMo",
                _ => method.ToString()
            };
        }

        private static string NormalizeGroupBy(string? groupBy)
        {
            string value = groupBy?.Trim().ToLowerInvariant() ?? "day";

            return value switch
            {
                "month" => "month",
                "year" => "year",
                _ => "day"
            };
        }

        private static (DateTime FromDate, DateTime ToDate) ResolveRange(
            DateTime? fromDate,
            DateTime? toDate,
            string? preset)
        {
            DateTime today = DateTime.Today;
            string normalizedPreset = preset?.Trim().ToLowerInvariant() ?? string.Empty;

            switch (normalizedPreset)
            {
                case "today":
                    return (today, today);
                case "7days":
                    return (today.AddDays(-6), today);
                case "year":
                    return (new DateTime(today.Year, 1, 1), today);
                case "month":
                    return (new DateTime(today.Year, today.Month, 1), today);
            }

            DateTime from = fromDate?.Date
                ?? new DateTime(today.Year, today.Month, 1);
            DateTime to = toDate?.Date ?? today;

            return (from, to);
        }

        private static void CreateOverviewSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("TongQuan");

            sheet.Cell(1, 1).Value = "BÁO CÁO TỔNG QUAN KHÁCH SẠN";
            sheet.Range(1, 1, 1, 4).Merge();
            sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 4).Style.Font.FontSize = 16;
            sheet.Range(1, 1, 1, 4).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            sheet.Cell(2, 1).Value = "Khoảng thời gian";
            sheet.Cell(2, 2).Value = model.PeriodText;
            sheet.Range(2, 2, 2, 4).Merge();

            string[] headers = { "Chỉ số", "Giá trị", "Đơn vị", "Ghi chú" };
            WriteHeader(sheet, 4, headers);

            int row = 5;
            WriteMetric(sheet, row++, "Doanh thu đã thu", model.TotalRevenue, "VNĐ", "Chỉ tính Payment = Paid", true);
            WriteMetric(sheet, row++, "Giao dịch đã thanh toán", model.PaidTransactionCount, "giao dịch", "", false);
            WriteMetric(sheet, row++, "Booking tạo trong kỳ", model.BookingCount, "booking", "Theo BookingDate", false);
            WriteMetric(sheet, row++, "Booking đã trả phòng", model.CheckedOutBookingCount, "booking", "Trong nhóm Booking tạo trong kỳ", false);
            WriteMetric(sheet, row++, "Booking đã hủy", model.CancelledBookingCount, "booking", "Trong nhóm Booking tạo trong kỳ", false);
            WriteMetric(sheet, row++, "Số hóa đơn", model.InvoiceCount, "hóa đơn", "Theo InvoiceDate", false);
            WriteMetric(sheet, row++, "Giá trị hóa đơn", model.InvoiceValue, "VNĐ", "Không đồng nghĩa đã thu tiền", true);
            WriteMetric(sheet, row++, "Tiền cọc ghi nhận", model.TotalDeposit, "VNĐ", "Booking tạo trong kỳ", true);
            WriteMetric(sheet, row++, "Tổng số phòng", model.TotalRooms, "phòng", "Trạng thái hiện tại", false);
            WriteMetric(sheet, row++, "Phòng trống", model.AvailableRooms, "phòng", "Trạng thái hiện tại", false);
            WriteMetric(sheet, row++, "Phòng đang sử dụng", model.OccupiedRooms, "phòng", "Trạng thái hiện tại", false);
            WriteMetric(sheet, row++, "Phòng bảo trì", model.MaintenanceRooms, "phòng", "Trạng thái hiện tại", false);
            WriteMetric(sheet, row++, "Đêm phòng sử dụng", model.OccupiedRoomNights, "đêm", "CheckedIn/CheckedOut trong kỳ", false);
            WriteMetric(sheet, row++, "Tỷ lệ sử dụng phòng", model.OccupancyRate, "%", "Loại trừ phòng đang Maintenance ở mẫu số", false);

            sheet.Range(4, 1, row - 1, 4).Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;
            sheet.Range(4, 1, row - 1, 4).Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            sheet.Columns().AdjustToContents();
            sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 27);
            sheet.Column(4).Width = Math.Max(sheet.Column(4).Width, 38);
            sheet.Column(4).Style.Alignment.WrapText = true;
        }

        private static void CreateRevenueSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("DoanhThu");

            sheet.Cell(1, 1).Value = "DOANH THU ĐÃ THU";
            sheet.Range(1, 1, 1, 4).Merge();
            FormatTitle(sheet.Range(1, 1, 1, 4));

            WriteHeader(sheet, 3, new[]
            {
                "Thời gian",
                "Số giao dịch Paid",
                "Doanh thu",
                "Doanh thu lũy kế"
            });

            int row = 4;
            foreach (var item in model.RevenueRows)
            {
                sheet.Cell(row, 1).Value = item.Label;
                sheet.Cell(row, 2).Value = item.TransactionCount;
                sheet.Cell(row, 3).Value = item.Revenue;
                sheet.Cell(row, 4).Value = item.CumulativeRevenue;
                ApplyMoney(sheet.Cell(row, 3));
                ApplyMoney(sheet.Cell(row, 4));
                row++;
            }

            sheet.Cell(row, 1).Value = "TỔNG";
            sheet.Cell(row, 2).Value = model.PaidTransactionCount;
            sheet.Cell(row, 3).Value = model.TotalRevenue;
            sheet.Cell(row, 4).Value = model.TotalRevenue;
            ApplyMoney(sheet.Cell(row, 3));
            ApplyMoney(sheet.Cell(row, 4));
            FormatTotal(sheet.Range(row, 1, row, 4));

            ApplyTableBorders(sheet, 3, row, 4);
            sheet.SheetView.FreezeRows(3);
            sheet.Columns().AdjustToContents();
        }

        private static void CreateInvoiceSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("HoaDon");

            sheet.Cell(1, 1).Value = "CƠ CẤU GIÁ TRỊ PHÁT SINH TRÊN HÓA ĐƠN";
            sheet.Range(1, 1, 1, 3).Merge();
            FormatTitle(sheet.Range(1, 1, 1, 3));

            WriteHeader(sheet, 3, new[]
            {
                "Hạng mục",
                "Giá trị",
                "Ghi chú"
            });

            int row = 4;
            foreach (var item in model.InvoiceCategories)
            {
                sheet.Cell(row, 1).Value = item.Label;
                sheet.Cell(row, 2).Value = item.Amount;
                sheet.Cell(row, 3).Value = "Giá trị phát sinh, không phải doanh thu đã thu";
                ApplyMoney(sheet.Cell(row, 2));
                row++;
            }

            sheet.Cell(row, 1).Value = "TỔNG GIÁ TRỊ HÓA ĐƠN";
            sheet.Cell(row, 2).Value = model.InvoiceValue;
            sheet.Cell(row, 3).Value = $"{model.InvoiceCount} hóa đơn trong kỳ";
            ApplyMoney(sheet.Cell(row, 2));
            FormatTotal(sheet.Range(row, 1, row, 3));

            ApplyTableBorders(sheet, 3, row, 3);
            sheet.Columns().AdjustToContents();
            sheet.Column(3).Width = Math.Max(sheet.Column(3).Width, 44);
        }

        private static void CreateBookingSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("Booking");

            sheet.Cell(1, 1).Value = "THỐNG KÊ BOOKING";
            sheet.Range(1, 1, 1, 5).Merge();
            FormatTitle(sheet.Range(1, 1, 1, 5));

            WriteHeader(sheet, 3, new[] { "Trạng thái", "Số lượng" });
            int row = 4;
            foreach (var item in model.BookingStatuses)
            {
                sheet.Cell(row, 1).Value = item.Label;
                sheet.Cell(row, 2).Value = item.Count;
                row++;
            }
            ApplyTableBorders(sheet, 3, row - 1, 2);

            int dailyStart = row + 2;
            WriteHeader(sheet, dailyStart, new[]
            {
                "Ngày",
                "Booking mới",
                "Check In",
                "Check Out",
                "Hủy*"
            });

            int dailyRow = dailyStart + 1;
            foreach (var item in model.BookingDailyRows)
            {
                sheet.Cell(dailyRow, 1).Value = item.Date;
                sheet.Cell(dailyRow, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                sheet.Cell(dailyRow, 2).Value = item.NewBookings;
                sheet.Cell(dailyRow, 3).Value = item.CheckIns;
                sheet.Cell(dailyRow, 4).Value = item.CheckOuts;
                sheet.Cell(dailyRow, 5).Value = item.CancelledBookings;
                dailyRow++;
            }

            if (dailyRow == dailyStart + 1)
            {
                sheet.Cell(dailyRow, 1).Value = "Không có dữ liệu";
                sheet.Range(dailyRow, 1, dailyRow, 5).Merge();
                dailyRow++;
            }

            ApplyTableBorders(sheet, dailyStart, dailyRow - 1, 5);
            sheet.Cell(dailyRow + 1, 1).Value =
                "* Project hiện chưa lưu thời điểm hủy (CancelledAt), nên số Booking hủy theo ngày được quy theo BookingDate.";
            sheet.Range(dailyRow + 1, 1, dailyRow + 1, 5).Merge();
            sheet.Range(dailyRow + 1, 1, dailyRow + 1, 5).Style.Alignment.WrapText = true;

            sheet.Columns().AdjustToContents();
            sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 22);
        }

        private static void CreateRoomSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("Phong");

            sheet.Cell(1, 1).Value = "BÁO CÁO SỬ DỤNG PHÒNG";
            sheet.Range(1, 1, 1, 5).Merge();
            FormatTitle(sheet.Range(1, 1, 1, 5));

            sheet.Cell(2, 1).Value = "Tỷ lệ sử dụng phòng";
            sheet.Cell(2, 2).Value = model.OccupancyRate / 100m;
            sheet.Cell(2, 2).Style.NumberFormat.Format = "0.00%";

            WriteHeader(sheet, 4, new[]
            {
                "Phòng",
                "Loại phòng",
                "Số lượt ở",
                "Số đêm sử dụng",
                "Giá trị tiền phòng"
            });

            int row = 5;
            foreach (var item in model.RoomRows)
            {
                sheet.Cell(row, 1).Value = item.RoomNumber;
                sheet.Cell(row, 2).Value = item.RoomTypeName;
                sheet.Cell(row, 3).Value = item.StayCount;
                sheet.Cell(row, 4).Value = item.OccupiedNights;
                sheet.Cell(row, 5).Value = item.RoomValue;
                ApplyMoney(sheet.Cell(row, 5));
                row++;
            }

            if (row == 5)
            {
                sheet.Cell(row, 1).Value = "Không có dữ liệu lưu trú trong kỳ";
                sheet.Range(row, 1, row, 5).Merge();
                row++;
            }

            ApplyTableBorders(sheet, 4, row - 1, 5);
            sheet.SheetView.FreezeRows(4);
            sheet.Columns().AdjustToContents();
        }

        private static void CreateServiceSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("DichVu");

            sheet.Cell(1, 1).Value = "BÁO CÁO DỊCH VỤ";
            sheet.Range(1, 1, 1, 5).Merge();
            FormatTitle(sheet.Range(1, 1, 1, 5));

            WriteHeader(sheet, 3, new[]
            {
                "Dịch vụ",
                "Danh mục",
                "Lượt phát sinh",
                "Số lượng",
                "Giá trị"
            });

            int row = 4;
            foreach (var item in model.ServiceRows)
            {
                sheet.Cell(row, 1).Value = item.ServiceName;
                sheet.Cell(row, 2).Value = item.Category;
                sheet.Cell(row, 3).Value = item.UsageCount;
                sheet.Cell(row, 4).Value = item.Quantity;
                sheet.Cell(row, 5).Value = item.Amount;
                ApplyMoney(sheet.Cell(row, 5));
                row++;
            }

            if (row == 4)
            {
                sheet.Cell(row, 1).Value = "Không có dữ liệu dịch vụ trong kỳ";
                sheet.Range(row, 1, row, 5).Merge();
                row++;
            }

            ApplyTableBorders(sheet, 3, row - 1, 5);
            sheet.SheetView.FreezeRows(3);
            sheet.Columns().AdjustToContents();
        }

        private static void CreatePaymentSheet(
            XLWorkbook workbook,
            ReportViewModel model)
        {
            var sheet = workbook.Worksheets.Add("ThanhToan");

            sheet.Cell(1, 1).Value = "CHI TIẾT THANH TOÁN ĐÃ THU";
            sheet.Range(1, 1, 1, 7).Merge();
            FormatTitle(sheet.Range(1, 1, 1, 7));

            WriteHeader(sheet, 3, new[]
            {
                "Thời gian",
                "Mã giao dịch",
                "Mã Booking",
                "Mã hóa đơn",
                "Khách hàng",
                "Phương thức",
                "Số tiền"
            });

            int row = 4;
            foreach (var item in model.PaymentDetails)
            {
                sheet.Cell(row, 1).Value = item.PaymentDate;
                sheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                sheet.Cell(row, 2).Value = item.TransactionCode;
                sheet.Cell(row, 3).Value = item.BookingCode;
                sheet.Cell(row, 4).Value = item.InvoiceCode;
                sheet.Cell(row, 5).Value = item.CustomerName;
                sheet.Cell(row, 6).Value = item.PaymentMethod;
                sheet.Cell(row, 7).Value = item.Amount;
                ApplyMoney(sheet.Cell(row, 7));
                row++;
            }

            sheet.Cell(row, 1).Value = "TỔNG";
            sheet.Range(row, 1, row, 6).Merge();
            sheet.Cell(row, 7).Value = model.TotalRevenue;
            ApplyMoney(sheet.Cell(row, 7));
            FormatTotal(sheet.Range(row, 1, row, 7));

            ApplyTableBorders(sheet, 3, row, 7);
            sheet.SheetView.FreezeRows(3);
            sheet.Columns().AdjustToContents();
        }

        private static void WriteHeader(
            IXLWorksheet sheet,
            int row,
            string[] headers)
        {
            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cell(row, col + 1).Value = headers[col];
            }

            var range = sheet.Range(row, 1, row, headers.Length);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void WriteMetric(
            IXLWorksheet sheet,
            int row,
            string label,
            decimal value,
            string unit,
            string note,
            bool money)
        {
            sheet.Cell(row, 1).Value = label;
            sheet.Cell(row, 2).Value = value;
            sheet.Cell(row, 3).Value = unit;
            sheet.Cell(row, 4).Value = note;

            if (money)
            {
                ApplyMoney(sheet.Cell(row, 2));
            }
        }

        private static void ApplyMoney(IXLCell cell)
        {
            cell.Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
        }

        private static void FormatTitle(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 16;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void FormatTotal(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        private static void ApplyTableBorders(
            IXLWorksheet sheet,
            int firstRow,
            int lastRow,
            int lastColumn)
        {
            if (lastRow < firstRow)
            {
                return;
            }

            var range = sheet.Range(firstRow, 1, lastRow, lastColumn);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
    }
}