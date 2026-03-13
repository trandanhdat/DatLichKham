using ClinicBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DatLichKham.Services
{
    public interface IDoctorService
    {
        Task<List<Doctor>> GetAllAsync();
        Task<Doctor?> GetByIdAsync(int id);
        Task<Doctor?> GetByUserIdAsync(int userId);
        Task<Doctor> CreateAsync(Doctor doctor);
        Task<bool> UpdateAsync(Doctor doctor);
        Task<bool> SoftDeleteAsync(int id);

        // New methods to support HomeController without DB access in controller
        Task<PaginatedList<Doctor>> GetPagedAsync(string? searchString, int pageNumber, int pageSize);
        Task<Doctor?> GetByIdWithUserAsync(int id);
    }

    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _context;

        public DoctorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Doctor>> GetAllAsync()
        {
            return await _context.Doctors
                .OrderBy(d => d.FullName)
                .ToListAsync();
        }

        public async Task<Doctor?> GetByIdAsync(int id)
        {
            return await _context.Doctors.FindAsync(id);
        }

        public async Task<Doctor?> GetByUserIdAsync(int userId)
        {
            return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<Doctor> CreateAsync(Doctor doctor)
        {
            doctor.CreatedAt = DateTime.Now;
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<bool> UpdateAsync(Doctor doctor)
        {
            var existing = await _context.Doctors.FindAsync(doctor.Id);
            if (existing == null) return false;

            existing.FullName = doctor.FullName;
            existing.Specialty = doctor.Specialty;
            existing.Email = doctor.Email;
            existing.Phone = doctor.Phone;
            existing.IsActive = doctor.IsActive;

            _context.Doctors.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return false;
            doctor.IsActive = false;
            _context.Doctors.Update(doctor);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- New implementations ---

        public async Task<PaginatedList<Doctor>> GetPagedAsync(string? searchString, int pageNumber, int pageSize)
        {
            var query = _context.Doctors.AsQueryable().Where(d => d.IsActive);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(d => d.FullName.Contains(searchString) || d.Specialty.Contains(searchString));
            }

            query = query.OrderBy(d => d.FullName);

            return await PaginatedList<Doctor>.CreateAsync(query.AsNoTracking(), pageNumber, pageSize);
        }

        public async Task<Doctor?> GetByIdWithUserAsync(int id)
        {
            return await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
    }
}