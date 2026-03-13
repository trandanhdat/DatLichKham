using ClinicBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DatLichKham.Services
{
    public interface IAppointmentService
    {
        Task<List<Appointment>> GetAllAsync();
        Task<List<Appointment>> GetByDoctorIdAsync(int doctorId);
        Task<List<Appointment>> GetByDoctorUserIdAsync(int doctorUserId);
        Task<Appointment?> GetByIdAsync(int id);
        Task<bool> UpdateStatusAsync(int id, string status);

        Task<List<Appointment>> GetByUserIdAsync(int userId);
        Task<(bool Success, string? Error)> CreateAsync(Appointment appointment, int userId);
        Task<Appointment?> GetByIdForUserAsync(int id, int userId);
        Task<(bool Success, string? Error)> CancelAsync(int id, int userId);

        // New: Update status with actor identity. Enforce that doctors can act only on their own appointments.
        Task<(bool Success, string? Error)> UpdateStatusAsync(int id, string status, int actorUserId, string actorRole);
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;

        public AppointmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Appointment>> GetAllAsync()
        {
            return await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetByDoctorIdAsync(int doctorId)
        {
            return await _context.Appointments
                .Include(a => a.User)
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetByDoctorUserIdAsync(int doctorUserId)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);
            if (doctor == null) return new List<Appointment>();
            return await GetByDoctorIdAsync(doctor.Id);
        }

        public async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _context.Appointments.FindAsync(id);
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return false;
            appointment.Status = status;
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Appointment>> GetByUserIdAsync(int userId)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(Appointment appointment, int userId)
        {
            if (appointment.AppointmentDate.Date < DateTime.Now.Date)
            {
                return (false, "Ngày khám phải từ hôm nay trở đi");
            }

            if (appointment.DoctorId <= 0)
            {
                return (false, "Vui lòng chọn bác sĩ.");
            }

            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.DoctorId == appointment.DoctorId &&
                    a.AppointmentDate.Date == appointment.AppointmentDate.Date &&
                    a.AppointmentTime == appointment.AppointmentTime &&
                    a.Status != "Cancelled");

            if (existingAppointment != null)
            {
                return (false, "Bác sĩ đã có lịch khám vào thời gian này. Vui lòng chọn giờ khác.");
            }

            appointment.UserId = userId;
            appointment.Status = "Pending";
            appointment.CreatedAt = DateTime.Now;

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<Appointment?> GetByIdForUserAsync(int id, int userId)
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        }

        public async Task<(bool Success, string? Error)> CancelAsync(int id, int userId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (appointment == null)
            {
                return (false, "Lịch khám không tồn tại.");
            }

            if (appointment.Status != "Pending")
            {
                return (false, "Chỉ có lịch ở trạng thái Pending mới được hủy.");
            }

            appointment.Status = "Cancelled";
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        /// <summary>
        /// Update appointment status with actor identity check.
        /// - If actorRole == "Doctor": actorUserId must match appointment.Doctor.UserId (doctor can act only on own appointments).
        /// - Doctors are allowed to set status to "Confirmed" (per requirement).
        /// - Admins or system actors can update any appointment.
        /// </summary>
        public async Task<(bool Success, string? Error)> UpdateStatusAsync(int id, string status, int actorUserId, string actorRole)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return (false, "Lịch khám không tồn tại.");

            if (string.Equals(actorRole, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                // ensure the actor is the doctor assigned to the appointment
                if (appointment.Doctor == null || appointment.Doctor.UserId != actorUserId)
                {
                    return (false, "Bạn không có quyền cập nhật lịch này.");
                }

                // doctor is allowed to confirm; additional rules can be added here
            }

            appointment.Status = status;
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}