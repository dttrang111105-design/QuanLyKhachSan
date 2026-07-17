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

        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer - Account (1-1)
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Account)
                .WithOne(a => a.Customer)
                .HasForeignKey<Customer>(c => c.AccountId);

            // Employee - Account (1-1)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Account)
                .WithOne(a => a.Employee)
                .HasForeignKey<Employee>(e => e.AccountId);

            // RoomType - Room (1-n)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId);

            // Booking - Customer (1-n)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId);

            // BookingDetail - Booking (1-n)
            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId);

            // BookingDetail - Room (1-n)
            modelBuilder.Entity<BookingDetail>()
                .HasOne(bd => bd.Room)
                .WithMany(r => r.BookingDetails)
                .HasForeignKey(bd => bd.RoomId);

            // RoomImage - Room (1-n)
            modelBuilder.Entity<RoomImage>()
                .HasOne(ri => ri.Room)
                .WithMany(r => r.RoomImages)
                .HasForeignKey(ri => ri.RoomId);

            // ServiceBooking - Booking (1-n)
            modelBuilder.Entity<ServiceBooking>()
                .HasOne(sb => sb.Booking)
                .WithMany(b => b.ServiceBookings)
                .HasForeignKey(sb => sb.BookingId);

            // ServiceBooking - Service (1-n)
            modelBuilder.Entity<ServiceBooking>()
                .HasOne(sb => sb.Service)
                .WithMany(s => s.ServiceBookings)
                .HasForeignKey(sb => sb.ServiceId);

            // Invoice - Booking (1-1)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId);

            // Payment - Invoice (1-n)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId);

            // Review - Customer (1-n)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId);

            // Review - Room (1-n)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Room)
                .WithMany(room => room.Reviews)
                .HasForeignKey(r => r.RoomId);
        }
    }
}
