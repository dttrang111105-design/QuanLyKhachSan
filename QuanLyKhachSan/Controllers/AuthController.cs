using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;
using QuanLyKhachSan.ViewModels.Account;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace QuanLyKhachSan.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AuthController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
        private async Task<string> CreateUniqueUsernameAsync(string email)
        {
            string emailName = email.Split('@')[0];
            string username = new string(emailName
                .Where(character => char.IsLetterOrDigit(character) || character == '_')
                .ToArray());
            if (string.IsNullOrWhiteSpace(username))
            {
                username = "googleuser";
            }
            if (username.Length > 30)
            {
                username = username[..30];
            }
            string originalUsername = username;
            int number = 1;
            while (await _context.Accounts.AnyAsync(account => account.Username == username))
            {
                username = $"{originalUsername}{number}";
                number++;
            }
            return username;
        }
        private async Task SignInAccountAsync(Account account, bool isPersistent = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.Username),
                new Claim(ClaimTypes.Email, account.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, account.Role.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal, authenticationProperties);
        }

        private IActionResult RedirectAfterLogin(Account account, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                if (account.Role == UserRole.Customer &&
                    returnUrl.Contains("/Home/Dashboard", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Home");
                }
                return LocalRedirect(returnUrl);
            }
            if (account.Role == UserRole.Customer)
            {
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Dashboard", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var existed = await _context.Accounts.AnyAsync(account =>
                account.Username == model.Username ||
                account.Email == model.Email);
            if (existed)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc Email đã tồn tại");
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

            string safeFullName = System.Net.WebUtility.HtmlEncode(customer.FullName);
            string safeUsername = System.Net.WebUtility.HtmlEncode(account.Username);

            string emailBody = $@"
                <div style='font-family:Arial,sans-serif;line-height:1.6;color:#222'>
                    <h2 style='margin-bottom:12px'>Đăng ký tài khoản thành công</h2>
                    <p>Xin chào <strong>{safeFullName}</strong>,</p>
                    <p>Tài khoản của bạn tại Luxury Hotel đã được tạo thành công.</p>
                    <table style='border-collapse:collapse;margin:16px 0'>
                        <tr>
                            <td style='padding:6px 16px 6px 0'><strong>Tên đăng nhập:</strong></td>
                            <td style='padding:6px 0'>{safeUsername}</td>
                        </tr>
                        <tr>
                            <td style='padding:6px 16px 6px 0'><strong>Email:</strong></td>
                            <td style='padding:6px 0'>{System.Net.WebUtility.HtmlEncode(customer.Email)}</td>
                        </tr>
                    </table>
                    <p>Bạn có thể đăng nhập và sử dụng các chức năng đặt phòng của hệ thống.</p>
                    <p style='margin-top:24px'>Cảm ơn bạn đã sử dụng dịch vụ của Luxury Hotel.</p>
                </div>";

            await _emailService.SendAsync(
                customer.Email,
                "Đăng ký tài khoản thành công - Luxury Hotel",
                emailBody);

            TempData["AuthSuccess"] = "Đăng ký tài khoản thành công. Bạn có thể đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(UserRole.Customer.ToString()))
                {
                    return RedirectToAction("Index", "Home");
                }
                if (User.IsInRole(UserRole.Admin.ToString()) ||
                    User.IsInRole(UserRole.Receptionist.ToString()))
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            string passwordHash = HashPassword(model.Password);
            var account = await _context.Accounts.FirstOrDefaultAsync(account =>
                account.Username == model.Username &&
                account.PasswordHash == passwordHash &&
                account.IsActive && !account.IsDeleted);
            if (account == null)
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }
            account.LastLogin = DateTime.Now;
            account.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await SignInAccountAsync(account);
            return RedirectAfterLogin(account, returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync("External");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            string redirectUrl = Url.Action(nameof(GoogleResponse), "Auth")!;
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                properties.Items["returnUrl"] = returnUrl;
            }
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var externalResult = await HttpContext.AuthenticateAsync("External");
            if (!externalResult.Succeeded || externalResult.Principal == null)
            {
                TempData["AuthError"] = "Không thể lấy thông tin đăng nhập từ Google.";
                return RedirectToAction(nameof(Login));
            }
            string? email = externalResult.Principal.FindFirstValue(ClaimTypes.Email);
            string? fullName = externalResult.Principal.FindFirstValue(ClaimTypes.Name);
            string? returnUrl = null;
            if (externalResult.Properties?.Items.TryGetValue("returnUrl", out string? savedReturnUrl) == true)
            {
                returnUrl = savedReturnUrl;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync("External");
                TempData["AuthError"] = "Tài khoản Google không cung cấp địa chỉ Email.";
                return RedirectToAction(nameof(Login));
            }
            string normalizedEmail = email.Trim().ToLower();
            var account = await _context.Accounts.FirstOrDefaultAsync(
                account => account.Email != null && account.Email.ToLower() == normalizedEmail);
            if (account != null && (account.IsDeleted || !account.IsActive))
            {
                await HttpContext.SignOutAsync("External");
                TempData["AuthError"] = "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động.";
                return RedirectToAction(nameof(Login));
            }
            bool isNewGoogleAccount = false;

            if (account == null)
            {
                string username = await CreateUniqueUsernameAsync(normalizedEmail);
                account = new Account
                {
                    Username = username,
                    Email = normalizedEmail,
                    PhoneNumber = "0000000000",
                    PasswordHash = HashPassword(Guid.NewGuid().ToString("N")),
                    Role = UserRole.Customer,
                    IsActive = true,
                    LastLogin = DateTime.Now
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();
                isNewGoogleAccount = true;
            }
            else
            {
                account.LastLogin = DateTime.Now;
                account.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Google Login trước đây chỉ tạo Account nên tài khoản Customer
            // có thể chưa có bản ghi Customer. BookingsController cần Customer
            // để tạo Booking, vì vậy tự tạo hồ sơ nếu còn thiếu.
            if (account.Role == UserRole.Customer)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c =>
                        c.AccountId == account.Id &&
                        !c.IsDeleted);

                if (customer == null)
                {
                    string customerName = string.IsNullOrWhiteSpace(fullName)
                        ? account.Username
                        : fullName.Trim();

                    customer = new Customer
                    {
                        AccountId = account.Id,
                        FullName = customerName,
                        Gender = string.Empty,
                        DateOfBirth = null,
                        Phone = account.PhoneNumber ?? string.Empty,
                        Email = normalizedEmail,
                        Address = string.Empty,
                        CitizenId = string.Empty,
                        CreatedAt = DateTime.Now,
                        IsDeleted = false
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();

                    if (isNewGoogleAccount)
                    {
                        string safeFullName = System.Net.WebUtility.HtmlEncode(customer.FullName);

                        string emailBody = $@"
                            <div style='font-family:Arial,sans-serif;line-height:1.6;color:#222'>
                                <h2 style='margin-bottom:12px'>Đăng nhập Google thành công</h2>
                                <p>Xin chào <strong>{safeFullName}</strong>,</p>
                                <p>Hồ sơ khách hàng của bạn tại Luxury Hotel đã được tạo thành công.</p>
                                <p>Bạn có thể sử dụng tài khoản Google này để đặt phòng và quản lý Booking.</p>
                                <p style='margin-top:24px'>
                                    Cảm ơn bạn đã sử dụng dịch vụ của Luxury Hotel.
                                </p>
                            </div>";

                        await _emailService.SendAsync(
                            normalizedEmail,
                            "Chào mừng bạn đến với Luxury Hotel",
                            emailBody);
                    }
                }
            }

            await HttpContext.SignOutAsync("External");
            await SignInAccountAsync(account);
            return RedirectAfterLogin(account, returnUrl);
        }
    }
}