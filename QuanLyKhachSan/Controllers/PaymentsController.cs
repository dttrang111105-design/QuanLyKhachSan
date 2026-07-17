using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels.Payment;

[Authorize(Roles = "Admin,Receptionist")]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;

    private readonly IConfiguration _configuration;

    public PaymentsController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // GET: PAYMENTS
    public async Task<IActionResult> Index()
    {
        var data = _context.Payments.Include(p => p.Invoice);

        return View(await data.ToListAsync());
    }

    // GET: PAYMENTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(m => m.Id == id);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    // GET: PAYMENTS/Create
    public async Task<IActionResult> Create(int invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
            return NotFound();

        bool paid = await _context.Payments
            .AnyAsync(x => x.InvoiceId == invoiceId &&
                           x.PaymentStatus == PaymentStatus.Paid);

        if (paid)
        {
            TempData["Error"] = "Hóa đơn này đã được thanh toán.";

            return RedirectToAction(
                "Details",
                "Invoices",
                new { id = invoiceId });
        }

        decimal amountNeedPay = invoice.TotalAmount - invoice.Booking.Deposit;

        if (amountNeedPay < 0)
            amountNeedPay = 0;

        var model = new PaymentViewModel
        {
            InvoiceId = invoice.Id,
            Amount = amountNeedPay
        };

        return View(model);
    }

    // POST: PAYMENTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            .FirstOrDefaultAsync(i => i.Id == model.InvoiceId);

        if (invoice == null)
        {
            return NotFound();
        }

        // Kiểm tra hóa đơn đã thanh toán chưa
        bool paid = await _context.Payments
            .AnyAsync(p => p.InvoiceId == model.InvoiceId &&
                           p.PaymentStatus == PaymentStatus.Paid);

        if (paid)
        {
            TempData["Error"] = "Hóa đơn này đã được thanh toán.";

            return RedirectToAction(
                "Details",
                "Invoices",
                new { id = model.InvoiceId });
        }

        // Tính số tiền thực tế cần thanh toán
        decimal amountNeedPay = invoice.TotalAmount - invoice.Booking.Deposit;

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
            Note = model.Note
        };

        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thanh toán thành công.";

        return RedirectToAction(
            "Details",
            "Invoices",
            new { id = invoice.Id });
    }

    // GET: PAYMENTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }
        return View(payment);
    }

    // POST: PAYMENTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("InvoiceId,Invoice,PaymentMethod,PaymentStatus,Amount,TransactionCode,PaymentDate,Note,Id,CreatedAt,UpdatedAt,IsDeleted")] Payment payment)
    {
        if (id != payment.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(payment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(payment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(payment);
    }

    // GET: PAYMENTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(m => m.Id == id);
        if (payment == null)
        {
            return NotFound();
        }

        return View(payment);
    }

    // POST: PAYMENTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PaymentExists(int? id)
    {
        return _context.Payments.Any(e => e.Id == id);
    }

    public async Task<IActionResult> PaymentQr(int invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(x => x.Id == invoiceId);

        if (invoice == null)
            return NotFound();

        decimal amount = invoice.TotalAmount - invoice.Booking.Deposit;

        if (amount < 0)
            amount = 0;

        string bankCode = _configuration["BankInfo:BankCode"];
        string accountNo = _configuration["BankInfo:AccountNo"];
        string accountName = _configuration["BankInfo:AccountName"];

        string qr =
            $"https://img.vietqr.io/image/{bankCode}-{accountNo}-compact2.png" +
            $"?amount={amount}" +
            $"&addInfo={invoice.InvoiceCode}" +
            $"&accountName={Uri.EscapeDataString(accountName)}";

        var model = new PaymentQrViewModel
        {
            InvoiceId = invoice.Id,
            InvoiceCode = invoice.InvoiceCode,
            Amount = amount,
            AccountName = accountName,
            AccountNo = accountNo,
            BankCode = bankCode,
            QrUrl = qr
        };

        return View(model);
    }

    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        bool paid = await _context.Payments
            .AnyAsync(x => x.InvoiceId == id &&
                           x.PaymentStatus == PaymentStatus.Paid);

        if (paid)
        {
            TempData["Error"] = "Hóa đơn đã thanh toán.";

            return RedirectToAction(
                "Details",
                "Invoices",
                new { id });
        }

        decimal amount = invoice.TotalAmount - invoice.Booking.Deposit;

        if (amount < 0)
            amount = 0;

        Payment payment = new()
        {
            InvoiceId = invoice.Id,
            Amount = amount,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Paid,
            PaymentDate = DateTime.Now,
            TransactionCode = "QR" + DateTime.Now.ToString("yyyyMMddHHmmss"),
            Note = "Thanh toán bằng VietQR"
        };

        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thanh toán thành công.";

        return RedirectToAction(
            "Details",
            "Invoices",
            new { id });
    }
}
