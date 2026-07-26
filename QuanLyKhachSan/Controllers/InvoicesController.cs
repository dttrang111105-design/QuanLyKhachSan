using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

[Authorize]
public class InvoicesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly InvoicePdfService _pdf;

    public InvoicesController(
        ApplicationDbContext context,
        InvoicePdfService pdf)
    {
        _context = context;
        _pdf = pdf;
    }


    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Index()
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return View(invoices);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Details(int id)
    {
        var invoice = await GetInvoiceDetailsQuery()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public IActionResult Create()
    {
        ViewData["BookingId"] = new SelectList(
            _context.Bookings
                .Where(b => !b.IsDeleted && b.Status == BookingStatus.CheckedOut),
            "Id",
            "BookingCode");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(
        [Bind("BookingId,DiscountPercent,TaxPercent")]
        Invoice invoice)
    {
        invoice.InvoiceCode =
            "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");

        invoice.InvoiceDate = DateTime.Now;
        invoice.CreatedAt = DateTime.Now;
        invoice.IsDeleted = false;

        ModelState.Remove(nameof(Invoice.InvoiceCode));

        if (!ModelState.IsValid)
        {
            await LoadBookingSelectList(invoice.BookingId);
            return View(invoice);
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.Id == invoice.BookingId &&
                !b.IsDeleted);

        if (booking == null)
        {
            return NotFound();
        }

        if (booking.Status != BookingStatus.CheckedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Chỉ được lập hóa đơn khi khách đã trả phòng.");

            await LoadBookingSelectList(invoice.BookingId);
            return View(invoice);
        }

        bool existedInvoice = await _context.Invoices
            .AnyAsync(i =>
                i.BookingId == invoice.BookingId &&
                !i.IsDeleted);

        if (existedInvoice)
        {
            ModelState.AddModelError(
                string.Empty,
                "Booking này đã có hóa đơn.");

            await LoadBookingSelectList(invoice.BookingId);
            return View(invoice);
        }

        decimal roomAmount = await _context.BookingDetails
            .Where(d =>
                d.BookingId == invoice.BookingId &&
                !d.IsDeleted)
            .SumAsync(d => d.TotalPrice);

        decimal serviceAmount = await _context.ServiceBookings
            .Where(s =>
                s.BookingId == invoice.BookingId &&
                !s.IsDeleted)
            .SumAsync(s => s.TotalPrice);

        decimal subTotal = roomAmount + serviceAmount;
        decimal discountAmount =
            subTotal * invoice.DiscountPercent / 100;
        decimal amountAfterDiscount = subTotal - discountAmount;
        decimal taxAmount =
            amountAfterDiscount * invoice.TaxPercent / 100;

        invoice.RoomAmount = roomAmount;
        invoice.ServiceAmount = serviceAmount;
        invoice.TotalAmount = amountAfterDiscount + taxAmount;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Tạo hóa đơn thành công.";

        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,DiscountPercent,TaxPercent")]
        Invoice form)
    {
        if (id != form.Id)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        if (form.DiscountPercent < 0 ||
            form.DiscountPercent > 100)
        {
            ModelState.AddModelError(
                nameof(Invoice.DiscountPercent),
                "Giảm giá phải nằm trong khoảng từ 0 đến 100%.");
        }

        if (form.TaxPercent < 0 ||
            form.TaxPercent > 100)
        {
            ModelState.AddModelError(
                nameof(Invoice.TaxPercent),
                "Thuế phải nằm trong khoảng từ 0 đến 100%.");
        }

        if (!ModelState.IsValid)
        {
            form.BookingId = invoice.BookingId;
            form.Booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == invoice.BookingId);

            form.InvoiceCode = invoice.InvoiceCode;
            form.InvoiceDate = invoice.InvoiceDate;
            form.RoomAmount = invoice.RoomAmount;
            form.ServiceAmount = invoice.ServiceAmount;
            form.TotalAmount = invoice.TotalAmount;
            form.CreatedAt = invoice.CreatedAt;
            form.UpdatedAt = invoice.UpdatedAt;
            form.IsDeleted = invoice.IsDeleted;

            return View(form);
        }

        decimal subTotal = invoice.RoomAmount + invoice.ServiceAmount;
        decimal discountAmount =
            subTotal * form.DiscountPercent / 100;
        decimal amountAfterDiscount = subTotal - discountAmount;
        decimal taxAmount =
            amountAfterDiscount * form.TaxPercent / 100;

        invoice.DiscountPercent = form.DiscountPercent;
        invoice.TaxPercent = form.TaxPercent;
        invoice.TotalAmount = amountAfterDiscount + taxAmount;
        invoice.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật hóa đơn thành công.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await GetInvoiceDetailsQuery()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        invoice.IsDeleted = true;
        invoice.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa hóa đơn thành công.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var invoice = await GetInvoiceDetailsQuery()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        byte[] pdf = _pdf.Generate(invoice);

        return File(
            pdf,
            "application/pdf",
            $"{invoice.InvoiceCode}.pdf");
    }


    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyInvoices()
    {
        var customer = await GetCurrentCustomer();

        if (customer == null)
        {
            return NotFound();
        }

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Booking)
                .ThenInclude(b => b.BookingDetails.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.Room)
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .Where(i =>
                !i.IsDeleted &&
                i.Booking != null &&
                !i.Booking.IsDeleted &&
                i.Booking.CustomerId == customer.Id)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return View(invoices);
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyDetails(int id)
    {
        var customer = await GetCurrentCustomer();

        if (customer == null)
        {
            return NotFound();
        }

        var invoice = await GetInvoiceDetailsQuery()
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                !i.IsDeleted &&
                i.Booking != null &&
                i.Booking.CustomerId == customer.Id);

        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyExportPdf(int id)
    {
        var customer = await GetCurrentCustomer();

        if (customer == null)
        {
            return NotFound();
        }

        var invoice = await GetInvoiceDetailsQuery()
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                !i.IsDeleted &&
                i.Booking != null &&
                i.Booking.CustomerId == customer.Id);

        if (invoice == null)
        {
            return NotFound();
        }

        byte[] pdf = _pdf.Generate(invoice);

        return File(
            pdf,
            "application/pdf",
            $"{invoice.InvoiceCode}.pdf");
    }


    private IQueryable<Invoice> GetInvoiceDetailsQuery()
    {
        return _context.Invoices
            .AsNoTracking()
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Booking)
                .ThenInclude(b => b.BookingDetails.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.Room)
            .Include(i => i.Booking)
                .ThenInclude(b => b.ServiceBookings.Where(s => !s.IsDeleted))
                    .ThenInclude(s => s.Service)
            .Include(i => i.Payments.Where(p => !p.IsDeleted));
    }

    private async Task<Customer?> GetCurrentCustomer()
    {
        string? username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _context.Customers
            .AsNoTracking()
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c =>
                !c.IsDeleted &&
                c.Account != null &&
                !c.Account.IsDeleted &&
                c.Account.Username == username);
    }

    private async Task LoadBookingSelectList(int selectedId)
    {
        ViewData["BookingId"] = new SelectList(
            await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    !b.IsDeleted &&
                    b.Status == BookingStatus.CheckedOut)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync(),
            "Id",
            "BookingCode",
            selectedId);
    }
}