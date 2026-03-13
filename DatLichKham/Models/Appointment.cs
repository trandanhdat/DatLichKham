using System.ComponentModel.DataAnnotations;

namespace ClinicBookingSystem.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        // Bỏ [Required] vì UserId được set từ Session, không từ form
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bác sĩ")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày khám")]
        [Display(Name = "Ngày khám")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ khám")]
        [Display(Name = "Giờ khám")]
        public TimeSpan AppointmentTime { get; set; }

        [StringLength(500)]
        [Display(Name = "Lý do khám")]
        public string? Reason { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public User? User { get; set; }
        public Doctor? Doctor { get; set; }
    }
}