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
    public class ReceptionistActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceptionistActivitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            int? employeeId,
            string? actionType)
        {
            if (fromDate.HasValue &&
                toDate.HasValue &&
                fromDate.Value.Date > toDate.Value.Date)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Từ ngày không được lớn hơn đến ngày.");
            }

            var activities = ModelState.IsValid
                ? await LoadActivitiesAsync(fromDate, toDate, employeeId, actionType)
                : new List<ReceptionistActivity>();

            var revenuePayments = ModelState.IsValid
                ? await LoadRevenuePaymentsAsync(fromDate, toDate, actionType)
                : new List<Payment>();

            var model = await BuildViewModelAsync(
                activities,
                revenuePayments,
                fromDate,
                toDate,
                employeeId,
                actionType);

            return View(model);
        }

        public async Task<IActionResult> ExportExcel(
            DateTime? fromDate,
            DateTime? toDate,
            int? employeeId,
            string? actionType)
        {
            if (fromDate.HasValue &&
                toDate.HasValue &&
                fromDate.Value.Date > toDate.Value.Date)
            {
                TempData["Error"] = "Từ ngày không được lớn hơn đến ngày.";
                return RedirectToAction(nameof(Index), new
                {
                    fromDate,
                    toDate,
                    employeeId,
                    actionType
                });
            }

            var activities = await LoadActivitiesAsync(
                fromDate,
                toDate,
                employeeId,
                actionType);

            var revenuePayments = await LoadRevenuePaymentsAsync(
                fromDate,
                toDate,
                actionType);

            var model = await BuildViewModelAsync(
                activities,
                revenuePayments,
                fromDate,
                toDate,
                employeeId,
                actionType);

            using var workbook = new XLWorkbook();

            CreateSummarySheet(workbook, model);
            CreateActivityDetailSheet(workbook, model);
            CreateDailyRevenueSheet(workbook, model);
            CreatePaymentDetailSheet(workbook, model);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                BuildFileName(fromDate, toDate));
        }

        private async Task<List<ReceptionistActivity>> LoadActivitiesAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? employeeId,
            string? actionType)
        {
            var query = _context.ReceptionistActivities
                .AsNoTracking()
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Account)
                .Include(a => a.Booking)
                .Include(a => a.Invoice)
                .Include(a => a.Payment)
                .Where(a => !a.IsDeleted)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                DateTime from = fromDate.Value.Date;
                query = query.Where(a => a.ActionTime >= from);
            }

            if (toDate.HasValue)
            {
                DateTime toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.ActionTime < toExclusive);
            }

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            string normalizedActionType = actionType?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(normalizedActionType))
            {
                query = query.Where(a => a.ActionType == normalizedActionType);
            }

            return await query
                .OrderByDescending(a => a.ActionTime)
                .ThenByDescending(a => a.Id)
                .ToListAsync();
        }

        private async Task<List<Payment>> LoadRevenuePaymentsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? actionType)
        {
            string normalizedActionType = actionType?.Trim() ?? string.Empty;

            // Nếu lọc một hoạt động khác Payment thì phần doanh thu
            // cũng tuân theo chính bộ lọc đang chọn.
            if (!string.IsNullOrWhiteSpace(normalizedActionType) &&
                normalizedActionType != "Payment")
            {
                return new List<Payment>();
            }

            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Booking)
                .Where(p =>
                    !p.IsDeleted &&
                    p.PaymentStatus == PaymentStatus.Paid)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                DateTime from = fromDate.Value.Date;
                query = query.Where(p => p.PaymentDate >= from);
            }

            if (toDate.HasValue)
            {
                DateTime toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(p => p.PaymentDate < toExclusive);
            }

            return await query
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.Id)
                .ToListAsync();
        }

        private async Task<ReceptionistActivityViewModel> BuildViewModelAsync(
            List<ReceptionistActivity> activities,
            List<Payment> revenuePayments,
            DateTime? fromDate,
            DateTime? toDate,
            int? employeeId,
            string? actionType)
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Account)
                .Where(e =>
                    !e.IsDeleted &&
                    e.Account != null &&
                    !e.Account.IsDeleted &&
                    (e.Account.Role == UserRole.Receptionist ||
                     e.Account.Role == UserRole.Admin))
                .OrderBy(e => e.FullName)
                .Select(e => new ReceptionistActivityEmployeeOptionViewModel
                {
                    EmployeeId = e.Id,
                    EmployeeName = e.FullName
                })
                .ToListAsync();

            var rows = activities
                .Select(a => new ReceptionistActivityRowViewModel
                {
                    Id = a.Id,
                    ActionTime = a.ActionTime,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee?.FullName ?? "Nhân viên",
                    ActionType = a.ActionType,
                    ActionLabel = GetActionLabel(a.ActionType),
                    BookingId = a.BookingId,
                    BookingCode = a.Booking?.BookingCode,
                    InvoiceId = a.InvoiceId,
                    InvoiceCode = a.Invoice?.InvoiceCode,
                    InvoiceTotalAmount = a.Invoice?.TotalAmount,
                    PaymentId = a.PaymentId,
                    PaymentAmount = a.Payment != null &&
                                    !a.Payment.IsDeleted &&
                                    a.Payment.PaymentStatus == PaymentStatus.Paid
                        ? a.Payment.Amount
                        : null,
                    Description = a.Description
                })
                .ToList();

            // Gắn từng Payment Paid với lễ tân đã xử lý.
            // Ưu tiên PaymentId (dữ liệu mới).
            // Với Activity cũ chưa có PaymentId, fallback theo cùng Invoice
            // và thời gian ghi nhận gần PaymentDate nhất (<= 10 phút).
            var paymentRows = BuildPaymentRows(
                revenuePayments,
                activities);

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                paymentRows = paymentRows
                    .Where(x => x.EmployeeId == employeeId.Value)
                    .ToList();
            }

            var summaries = rows
                .GroupBy(x => new
                {
                    x.EmployeeId,
                    x.EmployeeName
                })
                .Select(group => new ReceptionistActivitySummaryViewModel
                {
                    EmployeeId = group.Key.EmployeeId,
                    EmployeeName = group.Key.EmployeeName,
                    CreateBookingCount = group.Count(x => x.ActionType == "CreateBooking"),
                    CheckInCount = group.Count(x => x.ActionType == "CheckIn"),
                    CheckOutCount = group.Count(x => x.ActionType == "CheckOut"),
                    PaymentCount = group.Count(x => x.ActionType == "Payment"),
                    CancelBookingCount = group.Count(x => x.ActionType == "CancelBooking"),
                    TotalCount = group.Count(),
                    RevenueAmount = paymentRows
                        .Where(p => p.EmployeeId == group.Key.EmployeeId)
                        .Sum(p => p.Amount)
                })
                .OrderByDescending(x => x.TotalCount)
                .ThenBy(x => x.EmployeeName)
                .ToList();

            var dailyRevenues = paymentRows
                .Where(p => p.EmployeeId.HasValue)
                .GroupBy(p => new
                {
                    Date = p.PaymentDate.Date,
                    EmployeeId = p.EmployeeId!.Value,
                    p.EmployeeName
                })
                .Select(group => new ReceptionistDailyRevenueViewModel
                {
                    Date = group.Key.Date,
                    EmployeeId = group.Key.EmployeeId,
                    EmployeeName = group.Key.EmployeeName,
                    TransactionCount = group.Count(),
                    RevenueAmount = group.Sum(x => x.Amount)
                })
                .OrderBy(x => x.EmployeeName)
                .ThenBy(x => x.Date)
                .ToList();

            // Lũy kế riêng cho từng lễ tân.
            foreach (var employeeGroup in dailyRevenues.GroupBy(x => x.EmployeeId))
            {
                decimal cumulative = 0;

                foreach (var day in employeeGroup.OrderBy(x => x.Date))
                {
                    cumulative += day.RevenueAmount;
                    day.CumulativeRevenue = cumulative;
                }
            }

            decimal totalRevenue = paymentRows.Sum(x => x.Amount);
            decimal staffHandledRevenue = paymentRows
                .Where(x => x.EmployeeId.HasValue)
                .Sum(x => x.Amount);

            return new ReceptionistActivityViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                EmployeeId = employeeId,
                ActionType = actionType?.Trim(),
                TotalActivities = rows.Count,
                CreateBookingCount = rows.Count(x => x.ActionType == "CreateBooking"),
                CheckInCount = rows.Count(x => x.ActionType == "CheckIn"),
                CheckOutCount = rows.Count(x => x.ActionType == "CheckOut"),
                PaymentCount = rows.Count(x => x.ActionType == "Payment"),
                CancelBookingCount = rows.Count(x => x.ActionType == "CancelBooking"),
                TotalRevenue = totalRevenue,
                StaffHandledRevenue = staffHandledRevenue,
                UnassignedRevenue = totalRevenue - staffHandledRevenue,
                Employees = employees,
                Summaries = summaries,
                Activities = rows,
                DailyRevenues = dailyRevenues,
                RevenuePayments = paymentRows
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.PaymentId)
                    .ToList()
            };
        }

        private static List<ReceptionistPaymentRevenueViewModel> BuildPaymentRows(
            List<Payment> payments,
            List<ReceptionistActivity> activities)
        {
            var paymentActivities = activities
                .Where(a => a.ActionType == "Payment")
                .OrderBy(a => a.ActionTime)
                .ThenBy(a => a.Id)
                .ToList();

            var usedActivityIds = new HashSet<int>();
            var rows = new List<ReceptionistPaymentRevenueViewModel>();

            foreach (var payment in payments
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.Id))
            {
                ReceptionistActivity? activity = paymentActivities
                    .FirstOrDefault(a =>
                        !usedActivityIds.Contains(a.Id) &&
                        a.PaymentId.HasValue &&
                        a.PaymentId.Value == payment.Id);

                if (activity == null)
                {
                    activity = paymentActivities
                        .Where(a =>
                            !usedActivityIds.Contains(a.Id) &&
                            !a.PaymentId.HasValue &&
                            a.InvoiceId.HasValue &&
                            a.InvoiceId.Value == payment.InvoiceId)
                        .Select(a => new
                        {
                            Activity = a,
                            Difference = Math.Abs(
                                (a.ActionTime - payment.PaymentDate).TotalMinutes)
                        })
                        .Where(x => x.Difference <= 10)
                        .OrderBy(x => x.Difference)
                        .ThenBy(x => x.Activity.Id)
                        .Select(x => x.Activity)
                        .FirstOrDefault();
                }

                if (activity != null)
                {
                    usedActivityIds.Add(activity.Id);
                }

                rows.Add(new ReceptionistPaymentRevenueViewModel
                {
                    PaymentId = payment.Id,
                    PaymentDate = payment.PaymentDate,
                    TransactionCode = payment.TransactionCode ?? string.Empty,
                    EmployeeId = activity?.EmployeeId,
                    EmployeeName = activity?.Employee?.FullName ??
                        "Khách tự thanh toán / Chưa gắn lễ tân",
                    BookingId = payment.Invoice?.BookingId,
                    BookingCode = payment.Invoice?.Booking?.BookingCode ?? string.Empty,
                    InvoiceId = payment.InvoiceId,
                    InvoiceCode = payment.Invoice?.InvoiceCode ?? string.Empty,
                    PaymentMethod = GetPaymentMethodLabel(payment.PaymentMethod),
                    Amount = payment.Amount
                });
            }

            return rows;
        }

        private static string GetActionLabel(string actionType)
        {
            return actionType switch
            {
                "CreateBooking" => "Tạo Booking",
                "CheckIn" => "Check In",
                "CheckOut" => "Check Out",
                "Payment" => "Thanh toán",
                "CancelBooking" => "Hủy Booking",
                _ => actionType
            };
        }

        private static string GetPaymentMethodLabel(PaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.Cash => "Tiền mặt",
                PaymentMethod.BankTransfer => "Chuyển khoản",
                PaymentMethod.VNPay => "VNPay",
                PaymentMethod.Momo => "MoMo",
                _ => paymentMethod.ToString()
            };
        }

        private static void CreateSummarySheet(
            XLWorkbook workbook,
            ReceptionistActivityViewModel model)
        {
            var sheet = workbook.Worksheets.Add("TongQuan");

            sheet.Cell(1, 1).Value = "THỐNG KÊ HOẠT ĐỘNG LỄ TÂN";
            sheet.Range(1, 1, 1, 8).Merge();
            sheet.Range(1, 1, 1, 8).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 8).Style.Font.FontSize = 16;
            sheet.Range(1, 1, 1, 8).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            sheet.Cell(2, 1).Value = "Thời gian lọc";
            sheet.Cell(2, 2).Value = BuildPeriodText(model.FromDate, model.ToDate);
            sheet.Range(2, 2, 2, 8).Merge();

            sheet.Cell(3, 1).Value = model.EmployeeId.HasValue
                ? "Doanh thu nhân viên đã chọn"
                : "Tổng doanh thu Paid";
            sheet.Cell(3, 2).Value = model.TotalRevenue;
            sheet.Cell(3, 2).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";

            sheet.Cell(4, 1).Value = "Doanh thu gắn với lễ tân";
            sheet.Cell(4, 2).Value = model.StaffHandledRevenue;
            sheet.Cell(4, 2).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";

            sheet.Cell(5, 1).Value = "Chưa gắn lễ tân / khách tự thanh toán";
            sheet.Cell(5, 2).Value = model.UnassignedRevenue;
            sheet.Cell(5, 2).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";

            sheet.Range(3, 1, 5, 8).Style.Font.Bold = true;

            string[] headers =
            {
                "Lễ tân / Nhân viên",
                "Tạo Booking",
                "Check In",
                "Check Out",
                "Thanh toán",
                "Hủy Booking",
                "Tổng thao tác",
                "Doanh thu xử lý"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cell(7, col + 1).Value = headers[col];
            }

            var headerRange = sheet.Range(7, 1, 7, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            int row = 8;
            foreach (var item in model.Summaries)
            {
                sheet.Cell(row, 1).Value = item.EmployeeName;
                sheet.Cell(row, 2).Value = item.CreateBookingCount;
                sheet.Cell(row, 3).Value = item.CheckInCount;
                sheet.Cell(row, 4).Value = item.CheckOutCount;
                sheet.Cell(row, 5).Value = item.PaymentCount;
                sheet.Cell(row, 6).Value = item.CancelBookingCount;
                sheet.Cell(row, 7).Value = item.TotalCount;
                sheet.Cell(row, 8).Value = item.RevenueAmount;
                sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                row++;
            }

            sheet.Cell(row, 1).Value = "TỔNG NHÂN VIÊN";
            sheet.Cell(row, 2).Value = model.CreateBookingCount;
            sheet.Cell(row, 3).Value = model.CheckInCount;
            sheet.Cell(row, 4).Value = model.CheckOutCount;
            sheet.Cell(row, 5).Value = model.PaymentCount;
            sheet.Cell(row, 6).Value = model.CancelBookingCount;
            sheet.Cell(row, 7).Value = model.TotalActivities;
            sheet.Cell(row, 8).Value = model.StaffHandledRevenue;
            sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";

            var totalRange = sheet.Range(row, 1, row, 8);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            sheet.Range(7, 1, row, 8).Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;
            sheet.Range(7, 1, row, 8).Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            sheet.SheetView.FreezeRows(7);
            sheet.Columns().AdjustToContents();
            if (sheet.Column(1).Width < 28)
            {
                sheet.Column(1).Width = 28;
            }
        }

        private static void CreateActivityDetailSheet(
            XLWorkbook workbook,
            ReceptionistActivityViewModel model)
        {
            var sheet = workbook.Worksheets.Add("ChiTietHoatDong");

            string[] headers =
            {
                "STT",
                "Thời gian",
                "Lễ tân / Nhân viên",
                "Hoạt động",
                "Mã Booking",
                "Mã hóa đơn",
                "Giá trị hóa đơn",
                "Số tiền thanh toán",
                "Nội dung"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cell(1, col + 1).Value = headers[col];
            }

            var headerRange = sheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            int row = 2;
            int stt = 1;

            foreach (var item in model.Activities)
            {
                sheet.Cell(row, 1).Value = stt++;
                sheet.Cell(row, 2).Value = item.ActionTime;
                sheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                sheet.Cell(row, 3).Value = item.EmployeeName;
                sheet.Cell(row, 4).Value = item.ActionLabel;
                sheet.Cell(row, 5).Value = item.BookingCode ?? string.Empty;
                sheet.Cell(row, 6).Value = item.InvoiceCode ?? string.Empty;

                if (item.InvoiceTotalAmount.HasValue)
                {
                    sheet.Cell(row, 7).Value = item.InvoiceTotalAmount.Value;
                    sheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                }

                if (item.PaymentAmount.HasValue)
                {
                    sheet.Cell(row, 8).Value = item.PaymentAmount.Value;
                    sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                }

                sheet.Cell(row, 9).Value = item.Description ?? string.Empty;
                row++;
            }

            if (row > 2)
            {
                sheet.Range(1, 1, row - 1, 9).Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;
                sheet.Range(1, 1, row - 1, 9).Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
            if (sheet.Column(3).Width < 24)
            {
                sheet.Column(3).Width = 24;
            }
            sheet.Column(9).Width = 48;
            sheet.Column(9).Style.Alignment.WrapText = true;
        }

        private static void CreateDailyRevenueSheet(
            XLWorkbook workbook,
            ReceptionistActivityViewModel model)
        {
            var sheet = workbook.Worksheets.Add("DoanhThuTheoNgay");

            sheet.Cell(1, 1).Value = "DOANH THU THEO NGÀY CỦA TỪNG LỄ TÂN";
            sheet.Range(1, 1, 1, 5).Merge();
            sheet.Range(1, 1, 1, 5).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 5).Style.Font.FontSize = 16;
            sheet.Range(1, 1, 1, 5).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            string[] headers =
            {
                "Ngày",
                "Lễ tân / Nhân viên",
                "Số giao dịch",
                "Doanh thu trong ngày",
                "Lũy kế của lễ tân"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cell(3, col + 1).Value = headers[col];
            }

            var headerRange = sheet.Range(3, 1, 3, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            int row = 4;

            foreach (var item in model.DailyRevenues
                .OrderBy(x => x.Date)
                .ThenBy(x => x.EmployeeName))
            {
                sheet.Cell(row, 1).Value = item.Date;
                sheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                sheet.Cell(row, 2).Value = item.EmployeeName;
                sheet.Cell(row, 3).Value = item.TransactionCount;
                sheet.Cell(row, 4).Value = item.RevenueAmount;
                sheet.Cell(row, 5).Value = item.CumulativeRevenue;
                sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                row++;
            }

            sheet.Cell(row, 1).Value = "TỔNG DOANH THU LỄ TÂN";
            sheet.Range(row, 1, row, 3).Merge();
            sheet.Cell(row, 4).Value = model.StaffHandledRevenue;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
            sheet.Cell(row, 5).Value = model.StaffHandledRevenue;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";

            var totalRange = sheet.Range(row, 1, row, 5);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            sheet.Range(3, 1, row, 5).Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;
            sheet.Range(3, 1, row, 5).Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            sheet.SheetView.FreezeRows(3);
            sheet.Columns().AdjustToContents();
            if (sheet.Column(2).Width < 28)
            {
                sheet.Column(2).Width = 28;
            }
        }

        private static void CreatePaymentDetailSheet(
            XLWorkbook workbook,
            ReceptionistActivityViewModel model)
        {
            var sheet = workbook.Worksheets.Add("ChiTietThanhToan");

            string[] headers =
            {
                "STT",
                "Thời gian thanh toán",
                "Lễ tân / Người xử lý",
                "Mã giao dịch",
                "Mã Booking",
                "Mã hóa đơn",
                "Phương thức",
                "Số tiền"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                sheet.Cell(1, col + 1).Value = headers[col];
            }

            var headerRange = sheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            int row = 2;
            int stt = 1;

            foreach (var payment in model.RevenuePayments)
            {
                sheet.Cell(row, 1).Value = stt++;
                sheet.Cell(row, 2).Value = payment.PaymentDate;
                sheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                sheet.Cell(row, 3).Value = payment.EmployeeName;
                sheet.Cell(row, 4).Value = payment.TransactionCode;
                sheet.Cell(row, 5).Value = payment.BookingCode;
                sheet.Cell(row, 6).Value = payment.InvoiceCode;
                sheet.Cell(row, 7).Value = payment.PaymentMethod;
                sheet.Cell(row, 8).Value = payment.Amount;
                sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                row++;
            }

            sheet.Cell(row, 7).Value = "TỔNG DOANH THU";
            sheet.Cell(row, 8).Value = model.TotalRevenue;
            sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
            sheet.Range(row, 7, row, 8).Style.Font.Bold = true;
            sheet.Range(row, 7, row, 8).Style.Fill.BackgroundColor = XLColor.LightBlue;

            if (row >= 2)
            {
                sheet.Range(1, 1, row, 8).Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;
                sheet.Range(1, 1, row, 8).Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
            if (sheet.Column(3).Width < 28)
            {
                sheet.Column(3).Width = 28;
            }
        }

        private static string BuildPeriodText(
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                return $"{fromDate.Value:dd/MM/yyyy} - {toDate.Value:dd/MM/yyyy}";
            }

            if (fromDate.HasValue)
            {
                return $"Từ {fromDate.Value:dd/MM/yyyy}";
            }

            if (toDate.HasValue)
            {
                return $"Đến {toDate.Value:dd/MM/yyyy}";
            }

            return "Toàn bộ dữ liệu";
        }

        private static string BuildFileName(
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                return $"ThongKeLeTan_DoanhThu_{fromDate.Value:yyyyMMdd}_{toDate.Value:yyyyMMdd}.xlsx";
            }

            if (fromDate.HasValue)
            {
                return $"ThongKeLeTan_DoanhThu_Tu_{fromDate.Value:yyyyMMdd}.xlsx";
            }

            if (toDate.HasValue)
            {
                return $"ThongKeLeTan_DoanhThu_Den_{toDate.Value:yyyyMMdd}.xlsx";
            }

            return $"ThongKeLeTan_DoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }
    }
}