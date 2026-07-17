using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;

[Authorize(Roles = "Admin")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Danh sách nhân viên
    public async Task<IActionResult> Index()
    {
        var employees = await _context.Employees
            .Include(e => e.Account)
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.FullName)
            .ToListAsync();

        return View(employees);
    }

    // Chi tiết nhân viên
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                !e.IsDeleted);

        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    // Mở form thêm nhân viên
    public async Task<IActionResult> Create()
    {
        await LoadAvailableAccountsAsync();

        return View(new Employee
        {
            HireDate = DateTime.Today,
            Status = true
        });
    }

    // Lưu nhân viên
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "AccountId,FullName,Gender,DateOfBirth,Phone,Email," +
            "Address,Position,Salary,HireDate,Status,Avatar")]
        Employee employee)
    {
        // Form không gửi navigation property Account
        ModelState.Remove(nameof(Employee.Account));

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == employee.AccountId &&
                !a.IsDeleted &&
                a.IsActive &&
                (a.Role == UserRole.Admin ||
                 a.Role == UserRole.Receptionist));

        if (account == null)
        {
            ModelState.AddModelError(
                nameof(Employee.AccountId),
                "Vui lòng chọn tài khoản Admin hoặc Receptionist hợp lệ.");
        }

        var accountAlreadyUsed = await _context.Employees
            .AnyAsync(e =>
                e.AccountId == employee.AccountId &&
                !e.IsDeleted);

        if (accountAlreadyUsed)
        {
            ModelState.AddModelError(
                nameof(Employee.AccountId),
                "Tài khoản này đã được liên kết với nhân viên khác.");
        }

        if (employee.Salary < 0)
        {
            ModelState.AddModelError(
                nameof(Employee.Salary),
                "Lương không được nhỏ hơn 0.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAvailableAccountsAsync(employee.AccountId);

            return View(employee);
        }

        employee.CreatedAt = DateTime.Now;
        employee.IsDeleted = false;

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Thêm nhân viên thành công.";

        return RedirectToAction(nameof(Index));
    }

    // Mở form chỉnh sửa
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                !e.IsDeleted);

        if (employee == null)
        {
            return NotFound();
        }

        ViewBag.AccountUsername = employee.Account?.Username;

        return View(employee);
    }

    // Lưu chỉnh sửa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,FullName,Gender,DateOfBirth,Phone,Email," +
            "Address,Position,Salary,HireDate,Status,Avatar")]
        Employee model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        ModelState.Remove(nameof(Employee.Account));

        var employee = await _context.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                !e.IsDeleted);

        if (employee == null)
        {
            return NotFound();
        }

        if (model.Salary < 0)
        {
            ModelState.AddModelError(
                nameof(Employee.Salary),
                "Lương không được nhỏ hơn 0.");
        }

        if (!ModelState.IsValid)
        {
            model.AccountId = employee.AccountId;
            ViewBag.AccountUsername = employee.Account?.Username;

            return View(model);
        }

        employee.FullName = model.FullName;
        employee.Gender = model.Gender;
        employee.DateOfBirth = model.DateOfBirth;
        employee.Phone = model.Phone;
        employee.Email = model.Email;
        employee.Address = model.Address;
        employee.Position = model.Position;
        employee.Salary = model.Salary;
        employee.HireDate = model.HireDate;
        employee.Status = model.Status;
        employee.Avatar = model.Avatar;
        employee.UpdatedAt = DateTime.Now;

        if (employee.Account != null)
        {
            employee.Account.Email =
                model.Email ?? employee.Account.Email;

            employee.Account.PhoneNumber =
                model.Phone ?? employee.Account.PhoneNumber;

            employee.Account.IsActive = model.Status;
            employee.Account.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật nhân viên thành công.";

        return RedirectToAction(nameof(Index));
    }

    // Xác nhận xóa
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                !e.IsDeleted);

        if (employee == null)
        {
            return NotFound();
        }

        return View(employee);
    }

    // Xóa mềm nhân viên
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Account)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        employee.Status = false;
        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.Now;

        if (employee.Account != null)
        {
            employee.Account.IsActive = false;
            employee.Account.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã ngừng hoạt động nhân viên.";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAvailableAccountsAsync(
        int? selectedAccountId = null)
    {
        var usedAccountIds = await _context.Employees
            .Where(e => !e.IsDeleted)
            .Select(e => e.AccountId)
            .ToListAsync();

        var accounts = await _context.Accounts
            .Where(a =>
                !a.IsDeleted &&
                a.IsActive &&
                (a.Role == UserRole.Admin ||
                 a.Role == UserRole.Receptionist) &&
                (!usedAccountIds.Contains(a.Id) ||
                 a.Id == selectedAccountId))
            .OrderBy(a => a.Username)
            .ToListAsync();

        ViewData["AccountId"] = new SelectList(
            accounts,
            "Id",
            "Username",
            selectedAccountId);
    }
}