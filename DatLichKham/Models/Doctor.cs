using ClinicBookingSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ClinicBookingSystem.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên bác sĩ")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chuyên khoa")]
        [StringLength(100)]
        [Display(Name = "Chuyên khoa")]
        public string Specialty { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Appointment>? Appointments { get; set; }
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}