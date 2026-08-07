using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels.Profile;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace QuanLyKhachSan.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public ProfileController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        public async Task<IActionResult> Index()
        {
            int accountId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var account = await _context.Accounts
                .FirstOrDefaultAsync(x => x.Id == accountId);
            if (account == null)
                return NotFound();
            var model = new ProfileViewModel
            {
                Username = account.Username,
                Email = account.Email,
                Role = account.Role.ToString(),
                CreatedAt = account.CreatedAt
            };
            if (account.Role.ToString() == "Customer")
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (customer != null)
                {
                    model.FullName = customer.FullName;
                    model.Phone = customer.Phone;
                    model.Address = customer.Address;
                    model.DateOfBirth = customer.DateOfBirth;
                    model.Avatar = customer.Avatar;
                }
            }
            else
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (employee != null)
                {
                    model.FullName = employee.FullName;
                    model.Phone = employee.Phone;
                    model.Address = employee.Address;
                    model.DateOfBirth = employee.DateOfBirth;
                    model.Avatar = employee.Avatar;
                }
            }
            return View(model);
        }
        //get Edit
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Edit()
        {
            int accountId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var account = await _context.Accounts
                .FirstOrDefaultAsync(x => x.Id == accountId);
            if (account == null)
                return NotFound();
            var model = new EditProfileViewModel();
            model.Email = account.Email;
            if (account.Role == UserRole.Customer)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (customer != null)
                {
                    model.FullName = customer.FullName;
                    model.Phone = customer.Phone;
                    model.Address = customer.Address;
                    model.DateOfBirth = customer.DateOfBirth;
                    model.Avatar = customer.Avatar;
                }
            }
            else
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (employee != null)
                {
                    model.FullName = employee.FullName;
                    model.Phone = employee.Phone;
                    model.Address = employee.Address;
                    model.DateOfBirth = employee.DateOfBirth;
                    model.Avatar = employee.Avatar;
                }
            }
            return View(model);
        }
        //post Edit
        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            int accountId = int.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            // Lấy Account
            var account = await _context.Accounts
                .FirstOrDefaultAsync(x => x.Id == accountId);
            if (account == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(Index));
            }
            // Cập nhật Email Account
            account.Email = model.Email;
            //  CUSTOMER 
            if (account.Role.ToString() == "Customer")
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (customer == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction(nameof(Index));
                }
                customer.FullName = model.FullName;
                customer.Phone = model.Phone;
                customer.Address = model.Address;
                customer.Email = model.Email;
                customer.DateOfBirth = model.DateOfBirth;
                // Upload Avatar
                if (model.AvatarFile != null)
                {
                    string folder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "avatar");
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    string fileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(model.AvatarFile.FileName);
                    string path = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(stream);
                    }
                    customer.Avatar = "/uploads/avatar/" + fileName;
                }
            }
            // EMPLOYEE 
            else
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.AccountId == accountId);
                if (employee == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin nhân viên.";
                    return RedirectToAction(nameof(Index));
                }
                employee.FullName = model.FullName;
                employee.Phone = model.Phone;
                employee.Address = model.Address;
                employee.Email = model.Email;
                employee.DateOfBirth = model.DateOfBirth;
                // Upload Avatar
                if (model.AvatarFile != null)
                {
                    string folder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "avatar");
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    string fileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(model.AvatarFile.FileName);
                    string path = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(stream);
                    }
                    employee.Avatar = "/uploads/avatar/" + fileName;
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
        //đổi mật khẩu
        //get
        [Authorize(Roles = "Customer")]
        public IActionResult ChangePassword()
        {
            return View();
        }
        //post
        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
                return NotFound();
            if (account.PasswordHash != HashPassword(model.OldPassword))
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng.");
                return View(model);
            }
            account.PasswordHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}