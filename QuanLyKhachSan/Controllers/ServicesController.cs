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
            .AsNoTracking()
            .Include(x => x.ServiceBookings.Where(booking =>
                !booking.IsDeleted))
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
        int id,
        [Bind(
            "Id,ServiceName,Price,Category,ImageUrl," +
            "Description,IsAvailable")]
        Service model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        model.ServiceName =
            model.ServiceName?.Trim() ?? string.Empty;

        model.Category =
            model.Category?.Trim();

        model.ImageUrl =
            model.ImageUrl?.Trim();

        model.Description =
            model.Description?.Trim();

        if (string.IsNullOrWhiteSpace(model.ServiceName))
        {
            ModelState.AddModelError(
                nameof(Service.ServiceName),
                "Vui lòng nhập tên dịch vụ.");
        }

        if (model.Price < 0)
        {
            ModelState.AddModelError(
                nameof(Service.Price),
                "Giá dịch vụ không được âm.");
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (service == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.CreatedAt = service.CreatedAt;
            model.UpdatedAt = service.UpdatedAt;
            model.IsDeleted = service.IsDeleted;

            return View(model);
        }

        service.ServiceName = model.ServiceName;
        service.Price = model.Price;
        service.Category = model.Category;
        service.ImageUrl = model.ImageUrl;
        service.Description = model.Description;
        service.IsAvailable = model.IsAvailable;
        service.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(model.Id))
            {
                return NotFound();
            }

            throw;
        }

        TempData["Success"] = "Cập nhật dịch vụ thành công.";

        return RedirectToAction(
            nameof(Details),
            new { id = service.Id });
    }

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
            .Where(service =>
                service.IsAvailable &&
                !service.IsDeleted)
            .OrderBy(service => service.Category)
            .ThenBy(service => service.ServiceName)
            .ToListAsync();

        return View(services);
    }


    // MỞ FORM ĐẶT DỊCH VỤ THEO TÊN TỪ TRANG CHỦ

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> BookServiceByName(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            TempData["Error"] =
                "Không xác định được dịch vụ cần đặt.";

            return Redirect(
                (Url.Action("Index", "Home") ?? "/") +
                "#services"
            );
        }

        keyword = keyword.Trim().ToLower();

        var activeServices = await _context.Services
            .Where(x =>
                x.IsAvailable &&
                !x.IsDeleted)
            .ToListAsync();

        var service = activeServices.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(x.ServiceName) &&
             x.ServiceName.ToLower().Contains(keyword))
            ||
            (!string.IsNullOrWhiteSpace(x.Category) &&
             x.Category.ToLower().Contains(keyword)));

        if (service == null)
        {
            TempData["Error"] =
                "Dịch vụ này chưa được thêm hoặc đang tạm ngừng cung cấp.";

            return Redirect(
                (Url.Action("Index", "Home") ?? "/") +
                "#services"
            );
        }

        return RedirectToAction(
            nameof(BookService),
            new { id = service.Id }
        );
    }

    // KHÁCH HÀNG: MỞ FORM ĐẶT DỊCH VỤ

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> BookService(int id)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(service =>
                service.Id == id &&
                service.IsAvailable &&
                !service.IsDeleted);

        if (service == null)
        {
            TempData["Error"] =
                "Dịch vụ không tồn tại hoặc đang tạm ngừng hoạt động.";

            return RedirectToAction(nameof(CustomerServices));
        }

        var customer = await GetCurrentCustomerAsync();

        if (customer == null)
        {
            return Unauthorized();
        }

        var model = new CustomerBookServiceViewModel
        {
            ServiceId = service.Id,
            ServiceName = service.ServiceName,
            Description = service.Description,
            Price = service.Price,
            Quantity = 1,
            ActiveBookings =
                await GetActiveBookingsAsync(customer.Id)
        };

        return View(model);
    }

    // KHÁCH HÀNG: XÁC NHẬN ĐẶT DỊCH VỤ

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
                string.Empty,
                "Dịch vụ không tồn tại hoặc đã ngừng cung cấp."
            );
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
                "Booking không tồn tại hoặc không thuộc tài khoản của bạn."
            );
        }
        else if (booking.Status != BookingStatus.CheckedIn)
        {
            ModelState.AddModelError(
                nameof(model.BookingId),
                "Chỉ Booking đang nhận phòng mới được đặt dịch vụ."
            );
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

        var existingServiceBooking =
            await _context.ServiceBookings
                .FirstOrDefaultAsync(x =>
                    x.BookingId == model.BookingId &&
                    x.ServiceId == model.ServiceId &&
                    !x.IsDeleted);

        if (existingServiceBooking != null)
        {
            existingServiceBooking.Quantity += model.Quantity;
            existingServiceBooking.UnitPrice = service!.Price;

            existingServiceBooking.TotalPrice =
                existingServiceBooking.Quantity *
                existingServiceBooking.UnitPrice;

            existingServiceBooking.UpdatedAt = DateTime.Now;
        }
        else
        {
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
            }
        );
    }

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