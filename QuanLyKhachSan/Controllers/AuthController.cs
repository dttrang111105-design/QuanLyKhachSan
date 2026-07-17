using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.ViewModels.Account;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Enums;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;

namespace QuanLyKhachSan.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();

            var bytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(password));

            return Convert.ToBase64String(bytes);
        }
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET
        public IActionResult Register()
        {
            return View();
        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existed = await _context.Accounts.AnyAsync(x =>
                x.Username == model.Username ||
                x.Email == model.Email);

            if (existed)
            {
                ModelState.AddModelError("","Tên đăng nhập hoặc Email đã tồn tại");

                return View(model);
            }

            var account = new Account
            {
                Username = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = HashPassword(model.Password),
                Role = UserRole.Customer,
                IsActive = true
            };

            _context.Accounts.Add(account);

            await _context.SaveChangesAsync();

            var customer = new Customer
            {
                AccountId = account.Id,
                FullName = model.FullName,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Phone = model.PhoneNumber,
                Email = model.Email,
                Address = model.Address,
                CitizenId = model.CitizenId
            };

            _context.Customers.Add(customer);

            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET
        public IActionResult Login()
        {
            return View();
        }

        //post đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string passwordHash = HashPassword(model.Password);

            var account = await _context.Accounts
                .FirstOrDefaultAsync(x =>
                    x.Username == model.Username &&
                    x.PasswordHash == passwordHash &&
                    x.IsActive);

            if (account == null)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, account.Role.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            if (account.Role == UserRole.Customer)
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Dashboard", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        //đăng nhập bằng google
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action(nameof(GoogleResponse));

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(
                properties,
                GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return RedirectToAction("Login");

            // TODO:
            // Sau khi hoàn thiện Google Login,
            // tìm Account theo Email rồi lấy Role.

            // Ví dụ tạm thời:
            // if (account.Role == UserRole.Customer)
            //     return RedirectToAction("Index", "Home");

            return RedirectToAction("Dashboard", "Home");
        }
    }
}
