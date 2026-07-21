using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels.Payment;

[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentsController(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Index()
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return View(payments);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Details(int id)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Booking)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(int invoiceId)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i =>
                i.Id == invoiceId &&
                !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        bool paid = await _context.Payments
            .AnyAsync(p =>
                p.InvoiceId == invoiceId &&
                !p.IsDeleted &&
                p.PaymentStatus == PaymentStatus.Paid);

        if (paid)
        {
            TempData["Error"] = "Hóa đơn này đã được thanh toán.";

            return RedirectToAction(
                "Details",
                "Invoices",
                new { id = invoiceId });
        }

        decimal amountNeedPay = invoice.TotalAmount - invoice.Booking!.Deposit;

        if (amountNeedPay < 0)
        {
            amountNeedPay = 0;
        }

        return View(new PaymentViewModel
        {
            InvoiceId = invoice.Id,
            Amount = amountNeedPay
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(PaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var invoice = await _context.Invoices
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i =>
                i.Id == model.InvoiceId &&
                !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        bool paid = await _context.Payments
            .AnyAsync(p =>
                p.InvoiceId == model.InvoiceId &&
                !p.IsDeleted &&
                p.PaymentStatus == PaymentStatus.Paid);

        if (paid)
        {
            TempData["Error"] = "Hóa đơn này đã được thanh toán.";

            return RedirectToAction(
                "Details",
                "Invoices",
                new { id = model.InvoiceId });
        }

        decimal amountNeedPay = invoice.TotalAmount - invoice.Booking!.Deposit;

        if (amountNeedPay < 0)
        {
            amountNeedPay = 0;
        }

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = amountNeedPay,
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = PaymentStatus.Paid,
            PaymentDate = DateTime.Now,
            TransactionCode = "GD" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Note = model.Note,
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Thanh toán thành công.";

        return RedirectToAction(
            "Details",
            "Invoices",
            new { id = invoice.Id });
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, Payment form)
    {
        if (id != form.Id)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (payment == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        payment.PaymentMethod = form.PaymentMethod;
        payment.PaymentStatus = form.PaymentStatus;
        payment.Amount = form.Amount;
        payment.TransactionCode = form.TransactionCode;
        payment.PaymentDate = form.PaymentDate;
        payment.Note = form.Note;
        payment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (payment == null)
        {
            return NotFound();
        }

        payment.IsDeleted = true;
        payment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Khách hàng chỉ được mở QR của hóa đơn thuộc Booking của mình.
    [Authorize(Roles = "Customer,Admin,Receptionist")]
    public async Task<IActionResult> PaymentQr(int invoiceId)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
                    .ThenInclude(c => c.Account)
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(i =>
                i.Id == invoiceId &&
                !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && !CustomerOwnsInvoice(invoice))
        {
            return Forbid();
        }

        decimal paidAmount = invoice.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid)
            .Sum(p => p.Amount);

        decimal amount = invoice.TotalAmount - invoice.Booking!.Deposit - paidAmount;

        if (amount < 0)
        {
            amount = 0;
        }

        if (amount == 0)
        {
            TempData["Success"] = "Hóa đơn đã được thanh toán đầy đủ.";

            return RedirectAfterCustomerAction(invoice.Id);
        }

        string bankCode = _configuration["BankInfo:BankCode"] ?? string.Empty;
        string accountNo = _configuration["BankInfo:AccountNo"] ?? string.Empty;
        string accountName = _configuration["BankInfo:AccountName"] ?? string.Empty;

        string qr =
            $"https://img.vietqr.io/image/{bankCode}-{accountNo}-compact2.png" +
            $"?amount={amount:0}" +
            $"&addInfo={Uri.EscapeDataString(invoice.InvoiceCode)}" +
            $"&accountName={Uri.EscapeDataString(accountName)}";

        return View(new PaymentQrViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            Amount = amount,
            AccountName = accountName,
            AccountNo = accountNo,
            BankCode = bankCode,
            QrUrl = qr
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer,Admin,Receptionist")]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
                    .ThenInclude(c => c.Account)
            .Include(i => i.Payments.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(i =>
                i.Id == id &&
                !i.IsDeleted);

        if (invoice == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && !CustomerOwnsInvoice(invoice))
        {
            return Forbid();
        }

        decimal paidAmount = invoice.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid)
            .Sum(p => p.Amount);

        decimal amount = invoice.TotalAmount - invoice.Booking!.Deposit - paidAmount;

        if (amount < 0)
        {
            amount = 0;
        }

        if (amount == 0)
        {
            TempData["Error"] = "Hóa đơn đã được thanh toán đầy đủ.";
            return RedirectAfterCustomerAction(invoice.Id);
        }

        var payment = new Payment
        {
            InvoiceId = invoice.Id,
            Amount = amount,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Paid,
            PaymentDate = DateTime.Now,
            TransactionCode = "QR" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Note = "Thanh toán bằng VietQR",
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ghi nhận thanh toán QR thành công.";

        return RedirectAfterCustomerAction(invoice.Id);
    }

    private bool CustomerOwnsInvoice(Invoice invoice)
    {
        string? username = User.Identity?.Name;

        return !string.IsNullOrWhiteSpace(username) &&
               invoice.Booking?.Customer?.Account?.Username == username;
    }

    private IActionResult RedirectAfterCustomerAction(int invoiceId)
    {
        if (User.IsInRole("Customer"))
        {
            return RedirectToAction(
                "MyDetails",
                "Invoices",
                new { id = invoiceId });
        }

        return RedirectToAction(
            "Details",
            "Invoices",
            new { id = invoiceId });
    }
}