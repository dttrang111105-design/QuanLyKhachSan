using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan.Models;
namespace QuanLyKhachSan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceBooking> ServiceBookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<RoomChargeItem> RoomChargeItems { get; set; }
        public DbSet<RoomInspection> RoomInspections { get; set; }
        public DbSet<RoomInspectionDetail> RoomInspectionDetails { get; set; }
        public DbSet<RoomMiniBarItem> RoomMiniBarItems { get; set; }
        public DbSet<ReceptionistActivity> ReceptionistActivities { get; set; }
        public DbSet<Review> Reviews { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Customer - Account: 1-0..1
            // Khách tại quầy có thể không có tài khoản.
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Account)
                .WithOne(a => a.Customer)
                .HasForeignKey<Customer>(c => c.AccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            // Employee - Account: 1-1
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Account)
                .WithOne(a => a.Employee)
                .HasForeignKey<Employee>(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            // RoomType - Room: 1-n
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            // Booking - Customer: 1-n
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            // BookingDetail - Booking: 1-n
            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            // BookingDetail - Room: 1-n
            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Room)
                .WithMany(r => r.BookingDetails)
                .HasForeignKey(bd => bd.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            // RoomImage - Room: 1-n
            modelBuilder.Entity<RoomImage>()
                .HasOne(ri => ri.Room)
                .WithMany(r => r.RoomImages)
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            // ServiceBooking - Booking: 1-n
            modelBuilder.Entity<ServiceBooking>()
                .HasOne(sb => sb.Booking)
                .WithMany(b => b.ServiceBookings)
                .HasForeignKey(sb => sb.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            // ServiceBooking - Service: 1-n
            modelBuilder.Entity<ServiceBooking>()
                .HasOne(sb => sb.Service)
                .WithMany(s => s.ServiceBookings)
                .HasForeignKey(sb => sb.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            // Invoice - Booking: 1-1
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
            // InvoiceDetail - Invoice: 1-n
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Invoice)
                .WithMany(i => i.InvoiceDetails)
                .HasForeignKey(id => id.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            // RoomInspection - Booking: 1-n
            modelBuilder.Entity<RoomInspection>()
                .HasOne(ri => ri.Booking)
                .WithMany()
                .HasForeignKey(ri => ri.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            // RoomInspectionDetail - RoomInspection: 1-n
            modelBuilder.Entity<RoomInspectionDetail>()
                .HasOne(rid => rid.RoomInspection)
                .WithMany(ri => ri.RoomInspectionDetails)
                .HasForeignKey(rid => rid.RoomInspectionId)
                .OnDelete(DeleteBehavior.Cascade);
            // RoomInspectionDetail - Room: 1-n
            modelBuilder.Entity<RoomInspectionDetail>()
                .HasOne(rid => rid.Room)
                .WithMany()
                .HasForeignKey(rid => rid.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            // RoomInspectionDetail - RoomChargeItem: 1-n
            modelBuilder.Entity<RoomInspectionDetail>()
                .HasOne(rid => rid.RoomChargeItem)
                .WithMany()
                .HasForeignKey(rid => rid.RoomChargeItemId)
                .OnDelete(DeleteBehavior.Restrict);
            // RoomMiniBarItem - Room: 1-n
            modelBuilder.Entity<RoomMiniBarItem>()
                .HasOne(item => item.Room)
                .WithMany(room => room.RoomMiniBarItems)
                .HasForeignKey(item => item.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            // RoomMiniBarItem - RoomChargeItem: 1-n
            modelBuilder.Entity<RoomMiniBarItem>()
                .HasOne(item => item.RoomChargeItem)
                .WithMany()
                .HasForeignKey(item => item.RoomChargeItemId)
                .OnDelete(DeleteBehavior.Restrict);
            // ReceptionistActivity - Employee: 1-n
            modelBuilder.Entity<ReceptionistActivity>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            // Giữ lịch sử nếu Booking bị xóa vật lý.
            modelBuilder.Entity<ReceptionistActivity>()
                .HasOne(a => a.Booking)
                .WithMany()
                .HasForeignKey(a => a.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
            // Giữ lịch sử nếu Invoice bị xóa vật lý.
            modelBuilder.Entity<ReceptionistActivity>()
                .HasOne(a => a.Invoice)
                .WithMany()
                .HasForeignKey(a => a.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
            // ReceptionistActivity - Payment: n-1
            // Restrict để giữ nguyên lịch sử giao dịch đã được nhân viên xử lý.
            modelBuilder.Entity<ReceptionistActivity>()
                .HasOne(a => a.Payment)
                .WithMany()
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            // Payment - Invoice: 1-n
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            // Review - Customer: 1-n
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            // Review - Room: 1-n
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Room)
                .WithMany(room => room.Reviews)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            // Các trường mã nghiệp vụ không được trùng.
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingCode)
                .IsUnique();
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceCode)
                .IsUnique();
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Username)
                .IsUnique();
            modelBuilder.Entity<RoomInspection>()
                .HasIndex(ri => ri.BookingId)
                .IsUnique();
            modelBuilder.Entity<RoomChargeItem>()
                .HasIndex(item => new { item.Category, item.Name })
                .IsUnique();
            modelBuilder.Entity<RoomMiniBarItem>()
                .HasIndex(item => new { item.RoomId, item.RoomChargeItemId })
                .IsUnique();
            modelBuilder.Entity<ReceptionistActivity>()
                .HasIndex(a => a.EmployeeId);
            modelBuilder.Entity<ReceptionistActivity>()
                .HasIndex(a => a.ActionTime);
            modelBuilder.Entity<ReceptionistActivity>()
                .HasIndex(a => a.ActionType);
            modelBuilder.Entity<ReceptionistActivity>()
                .HasIndex(a => a.PaymentId);
            DateTime seedDate = new DateTime(2026, 8, 7);
            modelBuilder.Entity<RoomChargeItem>().HasData(
                new RoomChargeItem { Id = 1, Name = "Rượu", Category = "MiniBar", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 2, Name = "Nước", Category = "MiniBar", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 3, Name = "Hoa quả", Category = "MiniBar", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 4, Name = "Snack", Category = "MiniBar", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 5, Name = "Khăn", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 6, Name = "Ga", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 7, Name = "Gối", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 8, Name = "Áo choàng", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 9, Name = "Máy sấy", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 10, Name = "TV", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false },
                new RoomChargeItem { Id = 11, Name = "Điều hòa", Category = "Asset", UsedPrice = 0, DamagedPrice = 0, LostPrice = 0, CreatedAt = seedDate, IsDeleted = false }
            );
        }
    }
}