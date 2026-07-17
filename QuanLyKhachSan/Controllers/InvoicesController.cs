using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;


[Authorize(Roles = "Admin,Receptionist")]
public class InvoicesController : Controller
{
    private readonly ApplicationDbContext _context;

    private readonly InvoicePdfService _pdf;

    public InvoicesController(ApplicationDbContext context, InvoicePdfService pdf)
    {
        _context = context;
        _pdf = pdf;
    }

    // GET: INVOICES
    public async Task<IActionResult> Index()
    {
        var data = _context.Invoices
            .Include(i => i.Booking);

        return View(await data.ToListAsync());
    }

    // GET: INVOICES/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var invoice = await _context.Invoices
         .Include(i => i.Booking)
             .ThenInclude(b => b.Customer)

         .Include(i => i.Booking)
             .ThenInclude(b => b.BookingDetails)
                 .ThenInclude(d => d.Room)

         .Include(i => i.Booking)
             .ThenInclude(b => b.ServiceBookings)
                 .ThenInclude(sb => sb.Service)

         .Include(i => i.Payments)

         .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        return View(invoice);
    }

    // GET: INVOICES/Create
    public IActionResult Create()
    {
        ViewData["BookingId"] = new SelectList(_context.Bookings, "Id", "BookingCode");

        return View();
    }

    // POST: INVOICES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("BookingId,DiscountPercent,TaxPercent")]
    Invoice invoice)
    {
        invoice.InvoiceCode = "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");

        ModelState.Remove(nameof(Invoice.InvoiceCode));

        if (!ModelState.IsValid)
        {
            ViewData["BookingId"] =
                new SelectList(_context.Bookings, "Id", "BookingCode", invoice.BookingId);

            return View(invoice);
        }

        var existedInvoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.BookingId == invoice.BookingId);

        // Lấy booking
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(x => x.Id == invoice.BookingId);

        if (booking == null)
        {
            return NotFound();
        }

        // Chỉ tạo hóa đơn khi khách đã trả phòng
        if (booking.Status != BookingStatus.CheckedOut)
        {
            ModelState.AddModelError("", "Chỉ được lập hóa đơn khi khách đã trả phòng.");

            ViewData["BookingId"] =
                new SelectList(_context.Bookings, "Id", "BookingCode", invoice.BookingId);

            return View(invoice);
        }

        if (existedInvoice != null)
        {
            ModelState.AddModelError("", "Booking này đã có hóa đơn.");

            ViewData["BookingId"] =
                new SelectList(_context.Bookings, "Id", "BookingCode", invoice.BookingId);

            return View(invoice);
        }

        var roomAmount = await _context.BookingDetails
            .Where(x => x.BookingId == invoice.BookingId)
            .SumAsync(x => x.TotalPrice);

        var serviceAmount = await _context.ServiceBookings
            .Where(x => x.BookingId == invoice.BookingId)
            .SumAsync(x => x.TotalPrice);

        invoice.RoomAmount = roomAmount;
        invoice.ServiceAmount = serviceAmount;

        var subtotal = roomAmount + serviceAmount;

        var discount = subtotal * invoice.DiscountPercent / 100;
        var tax = (subtotal - discount) * invoice.TaxPercent / 100;

        invoice.TotalAmount = subtotal - discount + tax;

        _context.Invoices.Add(invoice);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: INVOICES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
        {
            return NotFound();
        }
        return View(invoice);
    }

    // POST: INVOICES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("BookingId,Booking,InvoiceCode,InvoiceDate,RoomAmount,ServiceAmount,DiscountPercent,TaxPercent,TotalAmount,Payments,Id,CreatedAt,UpdatedAt,IsDeleted")] Invoice invoice)
    {
        if (id != invoice.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(invoice);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(invoice.Id))
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
        return View(invoice);
    }

    // GET: INVOICES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(m => m.Id == id);
        if (invoice == null)
        {
            return NotFound();
        }

        return View(invoice);
    }

    // POST: INVOICES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice != null)
        {
            _context.Invoices.Remove(invoice);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool InvoiceExists(int? id)
    {
        return _context.Invoices.Any(e => e.Id == id);
    }

    public async Task<IActionResult> ExportPdf(int id)
    {
        var invoice = await _context.Invoices

            .Include(i => i.Booking)

                .ThenInclude(b => b.Customer)

            .Include(i => i.Booking)

                .ThenInclude(b => b.BookingDetails)

                    .ThenInclude(d => d.Room)

            .Include(i => i.Booking)

                .ThenInclude(b => b.ServiceBookings)

                    .ThenInclude(s => s.Service)

            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        var pdf = _pdf.Generate(invoice);

        return File(
            pdf,
            "application/pdf",
            $"{invoice.InvoiceCode}.pdf");
    }
}
