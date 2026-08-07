using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Data;
using QuanLyKhachSan.Models;

namespace QuanLyKhachSan.Services
{
    public class ReceptionistActivityService
    {
        private readonly ApplicationDbContext _context;

        public ReceptionistActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Chỉ thêm Activity vào DbContext, không tự SaveChanges.
        // Activity sẽ được lưu cùng nghiệp vụ chính/transaction hiện tại.
        public async Task<ReceptionistActivity?> AddForCurrentUserAsync(
            ClaimsPrincipal user,
            string actionType,
            Booking? booking = null,
            Invoice? invoice = null,
            string? description = null,
            Payment? payment = null)
        {
            string? username = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var employee = await _context.Employees
                .Include(e => e.Account)
                .FirstOrDefaultAsync(e =>
                    !e.IsDeleted &&
                    e.Status &&
                    e.Account != null &&
                    !e.Account.IsDeleted &&
                    e.Account.IsActive &&
                    e.Account.Username == username);

            if (employee == null)
            {
                return null;
            }

            var activity = new ReceptionistActivity
            {
                EmployeeId = employee.Id,
                Employee = employee,
                Booking = booking,
                Invoice = invoice,
                Payment = payment,
                ActionType = actionType.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                ActionTime = DateTime.Now,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.ReceptionistActivities.Add(activity);
            return activity;
        }
    }
}