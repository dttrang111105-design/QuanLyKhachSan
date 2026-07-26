using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;

[Authorize(Roles = "Admin")]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Include(customer => customer.Account)
            .Include(customer =>
                customer.Bookings.Where(booking =>
                    !booking.IsDeleted))
            .Include(customer =>
                customer.Reviews.Where(review =>
                    !review.IsDeleted))
            .Where(customer =>
                !customer.IsDeleted)
            .OrderByDescending(customer =>
                customer.CreatedAt)
            .ToListAsync();

        return View(customers);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .Include(x => x.Account)
            .Include(x => x.Bookings.Where(b => !b.IsDeleted))
            .Include(x => x.Reviews.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadAccountsAsync();
        return View(new Customer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "AccountId,FullName,Gender,DateOfBirth,Phone," +
            "Email,Address,CitizenId,Avatar")]
        Customer model)
    {
        NormalizeCustomer(model);
        ValidateCustomer(model);

        if (!ModelState.IsValid)
        {
            await LoadAccountsAsync(model.AccountId);
            return View(model);
        }

        model.CreatedAt = DateTime.Now;
        model.UpdatedAt = DateTime.Now;
        model.IsDeleted = false;

        _context.Customers.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Thêm khách hàng {model.FullName} thành công.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (customer == null)
        {
            return NotFound();
        }

        await LoadAccountsAsync(customer.AccountId);

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,AccountId,FullName,Gender,DateOfBirth,Phone," +
            "Email,Address,CitizenId,Avatar")]
        Customer model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        NormalizeCustomer(model);
        ValidateCustomer(model);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (customer == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadAccountsAsync(model.AccountId);
            return View(model);
        }

        customer.AccountId = model.AccountId;
        customer.FullName = model.FullName;
        customer.Gender = model.Gender;
        customer.DateOfBirth = model.DateOfBirth;
        customer.Phone = model.Phone;
        customer.Email = model.Email;
        customer.Address = model.Address;
        customer.CitizenId = model.CitizenId;
        customer.Avatar = model.Avatar;
        customer.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Cập nhật khách hàng {customer.FullName} thành công.";

        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (customer == null)
        {
            return NotFound();
        }

        customer.IsDeleted = true;
        customer.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Xóa khách hàng {customer.FullName} thành công.";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAccountsAsync(int? selectedId = null)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .ToListAsync();

        ViewData["AccountId"] = new SelectList(
            accounts,
            "Id",
            "Username",
            selectedId);
    }

    private static void NormalizeCustomer(Customer model)
    {
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Gender = model.Gender?.Trim();
        model.Phone = model.Phone?.Trim();
        model.Email = model.Email?.Trim();
        model.Address = model.Address?.Trim();
        model.CitizenId = model.CitizenId?.Trim();
        model.Avatar = model.Avatar?.Trim();
    }

    private void ValidateCustomer(Customer model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            ModelState.AddModelError(
                nameof(Customer.FullName),
                "Vui lòng nhập họ tên khách hàng.");
        }

        if (model.DateOfBirth.HasValue &&
            model.DateOfBirth.Value.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(Customer.DateOfBirth),
                "Ngày sinh không được lớn hơn ngày hiện tại.");
        }
    }

    private bool CustomerExists(int id)
    {
        return _context.Customers.Any(x =>
            x.Id == id &&
            !x.IsDeleted);
    }
}