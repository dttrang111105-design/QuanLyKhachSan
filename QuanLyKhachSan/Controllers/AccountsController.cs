using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyKhachSan.Controllers
{
    [Authorize(Roles = "Admi")]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var accounts = await _context.Accounts
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Employee)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.IsActive)
                .ThenBy(a => a.Username)
                .ToListAsync();

            return View(accounts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Accounts
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Account
            {
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,PasswordHash,Email,PhoneNumber,Role")] Account account)
        {
            account.Username = account.Username?.Trim() ?? string.Empty;
            account.Email = account.Email?.Trim() ?? string.Empty;
            account.PhoneNumber = account.PhoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(account.Username))
            {
                ModelState.AddModelError(nameof(account.Username), "Vui lòng nhập tên đăng nhập.");
            }

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                ModelState.AddModelError(nameof(account.PasswordHash), "Vui lòng nhập mật khẩu.");
            }
            else if (account.PasswordHash.Length < 6)
            {
                ModelState.AddModelError(nameof(account.PasswordHash), "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            bool usernameExists = await _context.Accounts
                .AnyAsync(a => a.Username == account.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(nameof(account.Username), "Tên đăng nhập đã tồn tại.");
            }

            bool emailExists = await _context.Accounts
                .AnyAsync(a => a.Email == account.Email);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(account.Email), "Email đã được sử dụng.");
            }

            if (!ModelState.IsValid)
            {
                return View(account);
            }

            account.PasswordHash = HashPassword(account.PasswordHash);
            account.IsActive = true;
            account.IsDeleted = false;
            account.CreatedAt = DateTime.Now;
            account.UpdatedAt = null;
            account.LastLogin = null;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tạo tài khoản {account.Username} thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (account == null)
            {
                return NotFound();
            }

            account.PasswordHash = string.Empty;
            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,PasswordHash,Email,PhoneNumber,Role,IsActive")] Account account)
        {
            if (id != account.Id)
            {
                return NotFound();
            }

            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (existingAccount == null)
            {
                return NotFound();
            }

            account.Username = account.Username?.Trim() ?? string.Empty;
            account.Email = account.Email?.Trim() ?? string.Empty;
            account.PhoneNumber = account.PhoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                ModelState.Remove(nameof(account.PasswordHash));
            }
            else if (account.PasswordHash.Length < 6)
            {
                ModelState.AddModelError(nameof(account.PasswordHash), "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            bool usernameExists = await _context.Accounts
                .AnyAsync(a => a.Id != id && a.Username == account.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(nameof(account.Username), "Tên đăng nhập đã tồn tại.");
            }

            bool emailExists = await _context.Accounts
                .AnyAsync(a => a.Id != id && a.Email == account.Email);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(account.Email), "Email đã được sử dụng.");
            }

            bool isCurrentAccount = string.Equals(existingAccount.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);

            if (isCurrentAccount && !account.IsActive)
            {
                ModelState.AddModelError(nameof(account.IsActive), "Bạn không thể khóa tài khoản đang đăng nhập.");
            }

            if (!ModelState.IsValid)
            {
                account.PasswordHash = string.Empty;
                return View(account);
            }

            existingAccount.Username = account.Username;
            existingAccount.Email = account.Email;
            existingAccount.PhoneNumber = account.PhoneNumber;
            existingAccount.Role = account.Role;
            existingAccount.IsActive = account.IsActive;
            existingAccount.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                existingAccount.PasswordHash = HashPassword(account.PasswordHash);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật tài khoản {existingAccount.Username} thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountExists(account.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Accounts
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (account == null)
            {
                return NotFound();
            }

            if (string.Equals(account.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            return View(account);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (account == null)
            {
                return NotFound();
            }

            if (string.Equals(account.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            account.IsActive = false;
            account.IsDeleted = true;
            account.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa tài khoản {account.Username}.";
            return RedirectToAction(nameof(Index));
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool AccountExists(int id)
        {
            return _context.Accounts.Any(a => a.Id == id && !a.IsDeleted);
        }
    }
}