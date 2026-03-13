using ClinicBookingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace DatLichKham.Services
{
    public interface IAccountService
    {
        Task<(bool Success, string? Error)> RegisterAsync(User user);
        Task<User?> AuthenticateAsync(string username, string password);
        void SignIn(User user);
        void SignOut();
        Task<User?> GetCurrentUserAsync();
        Task<User?> GetByIdAsync(int id);
        Task<(bool Success, string? Error)> UpdateProfileAsync(User updatedUser, int currentUserId);

        // Account management methods (admin)
        Task<List<User>> GetAllAsync();
        Task<(bool Success, string? Error)> CreateAccountAsync(User user, string password);
        Task<(bool Success, string? Error)> UpdateAccountAsync(User updatedUser, string? newPassword);
        Task<bool> DeleteAccountAsync(int id);

        // Create account and link to doctor
        Task<(bool Success, string? Error)> CreateDoctorAccountAsync(int doctorId, string username, string password);
        Task<bool> UsernameExistsAsync(string username);
    }

    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                return (false, "Vui lòng nhập tên đăng nhập.");

            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return (false, "Tên đăng nhập đã tồn tại.");

            user.Role = "User";
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        }

        public void SignIn(User user)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            session.SetInt32("UserId", user.Id);
            session.SetString("Username", user.Username ?? string.Empty);
            session.SetString("Role", user.Role ?? string.Empty);
            session.SetString("FullName", user.FullName ?? string.Empty);
        }

        public void SignOut()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var userId = session?.GetInt32("UserId");
            if (userId == null) return null;
            return await _context.Users.FindAsync(userId.Value);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(User updatedUser, int currentUserId)
        {
            if (currentUserId != updatedUser.Id)
                return (false, "Không có quyền cập nhật thông tin này.");

            var existing = await _context.Users.FindAsync(updatedUser.Id);
            if (existing == null) return (false, "Người dùng không tồn tại.");

            existing.FullName = updatedUser.FullName;
            existing.Email = updatedUser.Email;
            existing.Phone = updatedUser.Phone;

            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                existing.Password = updatedUser.Password;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // --- Admin account management implementations ---

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> CreateAccountAsync(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                return (false, "Vui lòng nhập tên đăng nhập.");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Vui lòng nhập mật khẩu.");

            if (await UsernameExistsAsync(user.Username))
                return (false, "Tên đăng nhập đã tồn tại.");

            user.Password = password;
            user.Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAccountAsync(User updatedUser, string? newPassword)
        {
            var existing = await _context.Users.FindAsync(updatedUser.Id);
            if (existing == null) return (false, "Người dùng không tồn tại.");

            if (!string.Equals(existing.Username, updatedUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                if (await UsernameExistsAsync(updatedUser.Username))
                    return (false, "Tên đăng nhập đã tồn tại.");
            }

            existing.Username = updatedUser.Username;
            existing.FullName = updatedUser.FullName;
            existing.Email = updatedUser.Email;
            existing.Phone = updatedUser.Phone;
            existing.Role = updatedUser.Role;

            if (!string.IsNullOrEmpty(newPassword))
            {
                existing.Password = newPassword;
            }

            try
            {
                _context.Users.Update(existing);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Users.AnyAsync(u => u.Id == updatedUser.Id))
                    return (false, "Người dùng không tồn tại.");
                throw;
            }
        }

        public async Task<bool> DeleteAccountAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string? Error)> CreateDoctorAccountAsync(int doctorId, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Vui lòng nhập tên đăng nhập.");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Vui lòng nhập mật khẩu.");

            var doctor = await _context.Doctors.FindAsync(doctorId);
            if (doctor == null)
                return (false, "Bác sĩ không tồn tại.");

            if (doctor.UserId != null)
                return (false, "Tài khoản cho bác sĩ này đã tồn tại.");

            if (await UsernameExistsAsync(username))
                return (false, "Tên đăng nhập đã tồn tại.");

            var user = new User
            {
                Username = username,
                Password = password,
                FullName = doctor.FullName ?? username,
                Email = doctor.Email ?? string.Empty,
                Phone = doctor.Phone ?? string.Empty,
                Role = "Doctor",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            doctor.UserId = user.Id;
            _context.Doctors.Update(doctor);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            return await _context.Users.AnyAsync(u => u.Username == username);
        }
    }
}