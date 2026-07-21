using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels;

[Authorize]
public class ServicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Index()
    {
        var services = await _context.Services
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        return View(services);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }


    [Authorize(Roles = "Admin,Receptionist")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(
        [Bind("ServiceName,Price,Category,ImageUrl,Description")]
        Service service)
    {
        if (!ModelState.IsValid)
        {
            return View(service);
        }

        service.IsAvailable = true;
        service.CreatedAt = DateTime.Now;
        service.IsDeleted = false;

        _context.Services.Add(service);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thêm dịch vụ thành công.";

        return RedirectToAction(nameof(Index));
    }

    // SỬA DỊCH VỤ

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind(
            "Id,ServiceName,Price,Category,ImageUrl," +
            "Description,IsAvailable,CreatedAt,IsDeleted")]
        Service service)
    {
        if (id == null || id != service.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(service);
        }

        var oldService = await _context.Services
            .FirstOrDefaultAsync(x => x.Id == id);

        if (oldService == null)
        {
            return NotFound();
        }

        oldService.ServiceName = service.ServiceName;
        oldService.Price = service.Price;
        oldService.Category = service.Category;
        oldService.ImageUrl = service.ImageUrl;
        oldService.Description = service.Description;
        oldService.IsAvailable = service.IsAvailable;
        oldService.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(service.Id))
            {
                return NotFound();
            }

            throw;
        }

        TempData["Success"] = "Cập nhật dịch vụ thành công.";

        return RedirectToAction(nameof(Index));
    }

    // XÓA DỊCH VỤ

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var service = await _context.Services.FindAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        service.IsAvailable = false;
        service.IsDeleted = true;
        service.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Xóa dịch vụ thành công.";

        return RedirectToAction(nameof(Index));
    }

    // KHÁCH HÀNG: XEM DANH SÁCH DỊCH VỤ

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CustomerServices()
    {
        var services = await _context.Services
            .Where(x =>
                x.IsAvailable &&
                !x.IsDeleted)
            .OrderBy(x => x.Category)
            .ThenBy(x => x.ServiceName)
            .ToListAsync();

        return View(services);
    }

    // KHÁCH HÀNG: MỞ FORM ĐẶT DỊCH VỤ
    // GET: Services/BookService/5

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> BookService(int id)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.IsAvailable &&
                !x.IsDeleted);

        if (service == null)
        {
            TempData["Error"] =
                "Dịch vụ không tồn tại hoặc đã ngừng cung cấp.";

            return RedirectToAction(nameof(CustomerServices));
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            return Unauthorized();
        }

        var activeBookings =
            await GetActiveBookingsAsync(customer.Id);

        var model = new CustomerBookServiceViewModel
        {
            ServiceId = service.Id,
            ServiceName = service.ServiceName,
            Description = service.Description,
            Price = service.Price,
            Quantity = 1,
            ActiveBookings = activeBookings
        };

        return View(model);
    }

    // KHÁCH HÀNG: XÁC NHẬN ĐẶT DỊCH VỤ
    // POST: Services/BookService

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> BookService(
        CustomerBookServiceViewModel model)
    {
        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            return Unauthorized();
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == model.ServiceId &&
                x.IsAvailable &&
                !x.IsDeleted);

        if (service == null)
        {
            ModelState.AddModelError(
                "",
                "Dịch vụ không tồn tại hoặc đã ngừng cung cấp.");
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(x =>
                x.Id == model.BookingId &&
                x.CustomerId == customer.Id &&
                !x.IsDeleted);

        if (booking == null)
        {
            ModelState.AddModelError(
                nameof(model.BookingId),
                "Booking không tồn tại hoặc không thuộc tài khoản của bạn.");
        }
        else if (booking.Status != BookingStatus.CheckedIn)
        {
            ModelState.AddModelError(
                nameof(model.BookingId),
                "Chỉ booking đang nhận phòng mới được đặt dịch vụ.");
        }

        if (!ModelState.IsValid)
        {
            if (service != null)
            {
                model.ServiceName = service.ServiceName;
                model.Description = service.Description;
                model.Price = service.Price;
            }

            model.ActiveBookings =
                await GetActiveBookingsAsync(customer.Id);

            return View(model);
        }

         // Kiểm tra dịch vụ này đã được đặt trong booking chưa.
        var existingServiceBooking =
            await _context.ServiceBookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == model.BookingId &&
                    x.ServiceId == model.ServiceId &&
                    !x.IsDeleted);

        if (existingServiceBooking != null)
        {
             // Đã có thì cộng thêm số lượng.
            existingServiceBooking.Quantity += model.Quantity;

            existingServiceBooking.UnitPrice = service!.Price;

            existingServiceBooking.TotalPrice =
                existingServiceBooking.Quantity *
                existingServiceBooking.UnitPrice;

            existingServiceBooking.UpdatedAt = DateTime.Now;
        }
        else
        {
             // Chưa có thì tạo ServiceBooking mới.
            var serviceBooking = new ServiceBooking
            {
                BookingId = model.BookingId,
                ServiceId = model.ServiceId,
                Quantity = model.Quantity,
                UnitPrice = service!.Price,
                TotalPrice = service.Price * model.Quantity,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.ServiceBookings.Add(serviceBooking);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Đặt dịch vụ {service!.ServiceName} thành công.";

        return RedirectToAction(
            "Details",
            "Bookings",
            new
            {
                id = model.BookingId
            });
    }

    // HÀM LẤY KHÁCH HÀNG ĐANG ĐĂNG NHẬP

    private async Task<Customer?> GetCurrentCustomerAsync()
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _context.Customers
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x =>
                x.Account != null &&
                x.Account.Username == username &&
                !x.IsDeleted);
    }

    // LẤY BOOKING ĐANG CHECKED IN CỦA KHÁCH

    private async Task<List<SelectListItem>>
        GetActiveBookingsAsync(int customerId)
    {
        var bookings = await _context.Bookings
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status == BookingStatus.CheckedIn &&
                !x.IsDeleted)
            .OrderByDescending(x => x.CheckInDate)
            .ToListAsync();

        return bookings
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),

                Text =
                    $"{x.BookingCode} | " +
                    $"{x.CheckInDate:dd/MM/yyyy} - " +
                    $"{x.CheckOutDate:dd/MM/yyyy}"
            })
            .ToList();
    }

    private bool ServiceExists(int? id)
    {
        return id != null &&
               _context.Services.Any(x => x.Id == id);
    }
}