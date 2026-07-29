using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Connect"));
});

builder.Services.AddScoped<InvoicePdfService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultSignInScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.LoginPath = "/Auth/Login";
            options.AccessDeniedPath = "/Auth/Login";

            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;

            options.Cookie.Name = "QuanLyKhachSan.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;
        })
    .AddCookie(
        "External",
        options =>
        {
            options.Cookie.Name =
                "QuanLyKhachSan.External";

            options.ExpireTimeSpan =
                TimeSpan.FromMinutes(10);

            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite =
                SameSiteMode.Lax;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;
        })
    .AddGoogle(
        GoogleDefaults.AuthenticationScheme,
        options =>
        {
            options.ClientId =
                builder.Configuration["Google:ClientId"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Google:ClientId");

            options.ClientSecret =
                builder.Configuration["Google:ClientSecret"]
                ?? throw new InvalidOperationException(
                    "Chưa cấu hình Google:ClientSecret");

            options.SignInScheme = "External";
            options.SaveTokens = true;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            options.CallbackPath =
                "/signin-google";

            options.Events.OnRemoteFailure = context =>
            {
                string errorMessage =
                    Uri.EscapeDataString(
                        "Đăng nhập Google thất bại hoặc đã bị hủy.");
                context.Response.Redirect(
                                    $"/Auth/Login?googleError={errorMessage}");

                context.HandleResponse();
                return Task.CompletedTask;
            };
        });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();