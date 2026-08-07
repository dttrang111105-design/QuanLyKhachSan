using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
[Authorize(Roles = "Receptionist,Admin")]
public class BookingDetailsController : Controller
{
    private readonly ApplicationDbContext _context;
    public BookingDetailsController(ApplicationDbContext context)
    {
        _context = context;
    }
    // GET: BOOKINGDETAILS
    public async Task<IActionResult> Index()
    {
        var bookingDetails = _context.BookingDetails
            .Include(b => b.Booking)
            .Include(b => b.Room);
        return View(await bookingDetails.ToListAsync());
    }

    // GET: BOOKINGDETAILS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var bookingdetail = await _context.BookingDetails
            .FirstOrDefaultAsync(m => m.Id == id);
        if (bookingdetail == null)
        {
            return NotFound();
        }
        return View(bookingdetail);
    }

    // GET: BOOKINGDETAILS/Create
    public IActionResult Create()
    {
        ViewData["BookingId"] = new SelectList(_context.Bookings, "Id", "BookingCode");
        ViewData["RoomId"] = new SelectList(_context.Rooms.Where(r => r.Status == RoomStatus.Available),"Id","RoomNumber");
        return View();
    }

    // POST: BOOKINGDETAILS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    [Bind("BookingId,RoomId")] BookingDetail bookingDetail)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == bookingDetail.BookingId);
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == bookingDetail.RoomId);
        if (booking == null || room == null)
        {
            return NotFound();
        }
        bool existed = booking.BookingDetails
            .Any(x => x.RoomId == bookingDetail.RoomId);
        if (existed)
        {
            ModelState.AddModelError("", "Phòng này đã tồn tại trong Booking.");
            return View(bookingDetail);
        }
        bool overlap = await _context.BookingDetails
            .Include(x => x.Booking)
            .AnyAsync(x =>
                x.RoomId == bookingDetail.RoomId &&
                x.Booking != null &&
                !x.Booking.IsDeleted &&
                booking.CheckInDate < x.Booking.CheckOutDate &&
                booking.CheckOutDate > x.Booking.CheckInDate &&
                x.Booking.Status != BookingStatus.Cancelled &&
                x.Booking.Status != BookingStatus.CheckedOut);
        if (overlap)
        {
            ModelState.AddModelError("", "Phòng đã được đặt trong khoảng thời gian này.");
            return View(bookingDetail);
        }
        bookingDetail.NumberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;
        if (bookingDetail.NumberOfNights <= 0)
        {
            bookingDetail.NumberOfNights = 1;
        }
        bookingDetail.PricePerNight = room.PriceDay;
        bookingDetail.DiscountPercent = 0;
        bookingDetail.TotalPrice = bookingDetail.PricePerNight * bookingDetail.NumberOfNights;

        bookingDetail.CreatedAt = DateTime.Now;
        bookingDetail.IsDeleted = false;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.BookingDetails.Add(bookingDetail);
            await _context.SaveChangesAsync();
            
            booking.TotalAmount = await _context.BookingDetails
                .Where(x => x.BookingId == booking.Id && !x.IsDeleted)
                .SumAsync(x => x.TotalPrice);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Thêm Booking Detail thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
            return View(bookingDetail);
        }
    }

    // GET: BOOKINGDETAILS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var bookingdetail = await _context.BookingDetails.FindAsync(id);
        if (bookingdetail == null)
        {
            return NotFound();
        }
        return View(bookingdetail);
    }

    // POST: BOOKINGDETAILS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("BookingId,Booking,RoomId,Room,PricePerNight,NumberOfNights,DiscountPercent,TotalPrice,Id,CreatedAt,UpdatedAt,IsDeleted")] BookingDetail bookingdetail)
    {
        if (id != bookingdetail.Id)
        {
            return NotFound();
        }
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(bookingdetail);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingDetailExists(bookingdetail.Id))
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
        return View(bookingdetail);
    }

    // GET: BOOKINGDETAILS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var bookingdetail = await _context.BookingDetails
            .FirstOrDefaultAsync(m => m.Id == id);
        if (bookingdetail == null)
        {
            return NotFound();
        }
        return View(bookingdetail);
    }

    // POST: BOOKINGDETAILS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var bookingdetail = await _context.BookingDetails.FindAsync(id);
        if (bookingdetail != null)
        {
            _context.BookingDetails.Remove(bookingdetail);
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    private bool BookingDetailExists(int? id)
    {
        return _context.BookingDetails.Any(e => e.Id == id);
    }
}
