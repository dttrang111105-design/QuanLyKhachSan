
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels.ServiceBooking;

[Authorize(Roles = "Admin,Receptionist")]
public class ServiceBookingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServiceBookingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: SERVICEBOOKINGS
    public async Task<IActionResult> Index()
    {
        var data = _context.ServiceBookings.Include(x => x.Booking).Include(x => x.Service);

        return View(await data.ToListAsync());
    }

    // GET: SERVICEBOOKINGS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var servicebooking = await _context.ServiceBookings.Include(x => x.Booking).Include(x => x.Service).FirstOrDefaultAsync(m => m.Id == id);
        if (servicebooking == null)
        {
            return NotFound();
        }

        return View(servicebooking);
    }

    // GET: SERVICEBOOKINGS/Create
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(x => x.ServiceBookings)
            .FirstOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
            return NotFound();

        if (booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = "Chỉ booking đang ở mới được sử dụng dịch vụ.";
            return RedirectToAction("Details", "Bookings", new { id = bookingId });
        }

        var model = new CreateServiceBookingViewModel
        {
            BookingId = bookingId,

            Services = await _context.Services
                .Where(x => x.IsAvailable)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.ServiceName} - {x.Price:N0} VNĐ"
                })
                .ToListAsync()
        };

        return View(model);
    }

    // POST: SERVICEBOOKINGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(CreateServiceBookingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Services = await _context.Services
                .Where(x => x.IsAvailable)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.ServiceName} - {x.Price:N0} VNĐ"
                })
                .ToListAsync();

            return View(model);
        }

        var booking = await _context.Bookings
    .FirstOrDefaultAsync(x => x.Id == model.BookingId);

        if (booking == null)
            return NotFound();

        if (booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] = "Booking đã trả phòng nên không thể thêm dịch vụ.";

            return RedirectToAction("Details", "Bookings",
                new { id = model.BookingId });
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x => x.Id == model.ServiceId);

        if (service == null)
            return NotFound();

        var existed = await _context.ServiceBookings
            .FirstOrDefaultAsync(x =>
                x.BookingId == model.BookingId &&
                x.ServiceId == model.ServiceId);

        if (existed != null)
        {
            existed.Quantity += model.Quantity;
            existed.TotalPrice = existed.Quantity * existed.UnitPrice;
        }
        else
        {
            var serviceBooking = new ServiceBooking
            {
                BookingId = model.BookingId,
                ServiceId = model.ServiceId,
                Quantity = model.Quantity,
                UnitPrice = service.Price,
                TotalPrice = service.Price * model.Quantity
            };

            _context.ServiceBookings.Add(serviceBooking);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã thêm dịch vụ thành công.";

        return RedirectToAction(
            "Details",
            "Bookings",
            new { id = model.BookingId });
    }

    // GET: SERVICEBOOKINGS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var servicebooking = await _context.ServiceBookings.FindAsync(id);
        if (servicebooking == null)
        {
            return NotFound();
        }
        return View(servicebooking);
    }

    // POST: SERVICEBOOKINGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
    int? id,
    [Bind("Id,BookingId,ServiceId,Quantity")]
    ServiceBooking serviceBooking)
    {
        if (id != serviceBooking.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceBooking.ServiceId);

            if (service == null)
            {
                return NotFound();
            }

            // Tính lại giá
            serviceBooking.UnitPrice = service.Price;
            serviceBooking.TotalPrice = service.Price * serviceBooking.Quantity;

            _context.Update(serviceBooking);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(serviceBooking);
    }

    // GET: SERVICEBOOKINGS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var servicebooking = await _context.ServiceBookings
            .Include(x => x.Booking)
            .Include(x => x.Service)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (servicebooking == null)
        {
            return NotFound();
        }

        return View(servicebooking);
    }

    // POST: SERVICEBOOKINGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var servicebooking = await _context.ServiceBookings.FindAsync(id);

        if (servicebooking != null)
        {
            _context.ServiceBookings.Remove(servicebooking);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ServiceBookingExists(int? id)
    {
        return _context.ServiceBookings.Any(e => e.Id == id);
    }
}
