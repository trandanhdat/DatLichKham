using Microsoft.EntityFrameworkCore;

namespace ClinicBookingSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Message>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
            // Seed dữ liệu mẫu
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin123", // Trong thực tế nên hash password
                    FullName = "Quản trị viên",
                    Email = "admin@clinic.com",
                    Phone = "0123456789",
                    Role = "Admin"
                },
                new User
                {
                    Id = 2,
                    Username = "user1",
                    Password = "user123",
                    FullName = "Nguyễn Văn A",
                    Email = "user1@email.com",
                    Phone = "0987654321",
                    Role = "User"
                }
            );

            modelBuilder.Entity<Doctor>().HasData(
                new Doctor
                {
                    Id = 1,
                    FullName = "BS. Trần Văn B",
                    Specialty = "Nội khoa",
                    Phone = "0912345678",
                    Email = "doctor1@clinic.com",
                    IsActive = true
                },
                new Doctor
                {
                    Id = 2,
                    FullName = "BS. Lê Thị C",
                    Specialty = "Da liễu",
                    Phone = "0923456789",
                    Email = "doctor2@clinic.com",
                    IsActive = true
                }
            );
        }
    }
}