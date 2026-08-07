using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Enums;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.ViewModels.Reviews;
[Authorize(Roles = "Customer")]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;
    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }
    // Danh sách đánh giá của khách hàng đang đăng nhập.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(review => review.Room)
                .ThenInclude(room => room!.RoomType)
            .Where(review =>
                review.CustomerId == customer.Id &&
                !review.IsDeleted)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync();
        return View(reviews);
    }
    // Xem chi tiết một đánh giá của chính khách hàng.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var review = await _context.Reviews
            .AsNoTracking()
            .Include(item => item.Room)
                .ThenInclude(room => room!.RoomType)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.CustomerId == customer.Id &&
                !item.IsDeleted);
        if (review == null)
        {
            return NotFound();
        }
        return View(review);
    }
    // Mở form đánh giá từ một booking đã checkout.
    [HttpGet]
    public async Task<IActionResult> Create(int bookingId, int roomId)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.Room)
                    .ThenInclude(room => room!.RoomType)
            .FirstOrDefaultAsync(item =>
                item.Id == bookingId &&
                item.CustomerId == customer.Id &&
                !item.IsDeleted);
        if (booking == null)
        {
            TempData["Error"] = "Không tìm thấy booking của bạn.";
            return RedirectToAction("MyBookings", "Bookings");
        }
        if (booking.Status != BookingStatus.CheckedOut)
        {
            TempData["Error"] = "Bạn chỉ có thể đánh giá sau khi đã trả phòng.";
            return RedirectToAction("MyBookings", "Bookings");
        }
        var bookingDetail = booking.BookingDetails
            .FirstOrDefault(detail =>
                detail.RoomId == roomId &&
                !detail.IsDeleted &&
                detail.Room != null &&
                !detail.Room.IsDeleted);
        if (bookingDetail?.Room == null)
        {
            TempData["Error"] = "Phòng này không thuộc booking đã chọn.";
            return RedirectToAction("MyBookings", "Bookings");
        }
        var existingReview = await _context.Reviews
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.CustomerId == customer.Id &&
                item.RoomId == roomId &&
                !item.IsDeleted);
        if (existingReview != null)
        {
            TempData["Info"] = "Bạn đã đánh giá phòng này. Bạn có thể chỉnh sửa đánh giá cũ.";
            return RedirectToAction(nameof(Edit), new { id = existingReview.Id });
        }
        var model = new ReviewFormViewModel
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            RoomId = bookingDetail.Room.Id,
            RoomNumber = bookingDetail.Room.RoomNumber,
            RoomTypeName = bookingDetail.Room.RoomType?.Name,
            Rating = 5
        };
        return View(model);
    }
    // Lưu đánh giá mới.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewFormViewModel model)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        model.Comment = string.IsNullOrWhiteSpace(model.Comment)
            ? null
            : model.Comment.Trim();
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.Room)
                    .ThenInclude(room => room!.RoomType)
            .FirstOrDefaultAsync(item =>
                item.Id == model.BookingId &&
                item.CustomerId == customer.Id &&
                item.Status == BookingStatus.CheckedOut &&
                !item.IsDeleted);
        var bookingDetail = booking?.BookingDetails
            .FirstOrDefault(detail =>
                detail.RoomId == model.RoomId &&
                !detail.IsDeleted &&
                detail.Room != null &&
                !detail.Room.IsDeleted);
        if (booking == null || bookingDetail?.Room == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Bạn chỉ được đánh giá phòng thuộc booking đã trả phòng.");
        }
        var existingReview = await _context.Reviews
            .AnyAsync(item =>
                item.CustomerId == customer.Id &&
                item.RoomId == model.RoomId &&
                !item.IsDeleted);
        if (existingReview)
        {
            ModelState.AddModelError(
                string.Empty,
                "Bạn đã đánh giá phòng này rồi.");
        }
        if (!ModelState.IsValid)
        {
            if (bookingDetail?.Room != null)
            {
                model.BookingCode = booking?.BookingCode;
                model.RoomNumber = bookingDetail.Room.RoomNumber;
                model.RoomTypeName = bookingDetail.Room.RoomType?.Name;
            }
            return View(model);
        }
        var review = new Review
        {
            CustomerId = customer.Id,
            RoomId = model.RoomId,
            Rating = model.Rating,
            Comment = model.Comment,
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cảm ơn bạn đã đánh giá phòng.";
        return RedirectToAction(nameof(Index));
    }
    // Mở form sửa đánh giá của chính khách hàng.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var review = await _context.Reviews
            .AsNoTracking()
            .Include(item => item.Room)
                .ThenInclude(room => room!.RoomType)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.CustomerId == customer.Id &&
                !item.IsDeleted);
        if (review?.Room == null)
        {
            return NotFound();
        }
        var model = new ReviewFormViewModel
        {
            Id = review.Id,
            RoomId = review.RoomId,
            RoomNumber = review.Room.RoomNumber,
            RoomTypeName = review.Room.RoomType?.Name,
            Rating = review.Rating,
            Comment = review.Comment
        };
        return View(model);
    }
    // Cập nhật đánh giá, không cho thay CustomerId hoặc RoomId.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReviewFormViewModel model)
    {
        if (model.Id != id)
        {
            return NotFound();
        }
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var review = await _context.Reviews
            .Include(item => item.Room)
                .ThenInclude(room => room!.RoomType)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.CustomerId == customer.Id &&
                !item.IsDeleted);
        if (review?.Room == null)
        {
            return NotFound();
        }
        model.Comment = string.IsNullOrWhiteSpace(model.Comment)
            ? null
            : model.Comment.Trim();
        if (!ModelState.IsValid)
        {
            model.RoomId = review.RoomId;
            model.RoomNumber = review.Room.RoomNumber;
            model.RoomTypeName = review.Room.RoomType?.Name;
            return View(model);
        }
        review.Rating = model.Rating;
        review.Comment = model.Comment;
        review.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật đánh giá thành công.";
        return RedirectToAction(nameof(Index));
    }
    // Trang xác nhận xóa.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var review = await _context.Reviews
            .AsNoTracking()
            .Include(item => item.Room)
                .ThenInclude(room => room!.RoomType)
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.CustomerId == customer.Id &&
                !item.IsDeleted);
        if (review == null)
        {
            return NotFound();
        }
        return View(review);
    }
    // Xóa mềm đánh giá.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await GetCurrentCustomerAsync();
        if (customer == null)
        {
            return Unauthorized();
        }
        var review = await _context.Reviews
            .FirstOrDefaultAsync(item =>
                item.Id == id &&
                item.CustomerId == customer.Id &&
                !item.IsDeleted);
        if (review == null)
        {
            return NotFound();
        }
        review.IsDeleted = true;
        review.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Xóa đánh giá thành công.";
        return RedirectToAction(nameof(Index));
    }
    private async Task<Customer?> GetCurrentCustomerAsync()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }
        return await _context.Customers
            .Include(customer => customer.Account)
            .FirstOrDefaultAsync(customer =>
                !customer.IsDeleted &&
                customer.Account != null &&
                !customer.Account.IsDeleted &&
                customer.Account.Username == username);
    }
}