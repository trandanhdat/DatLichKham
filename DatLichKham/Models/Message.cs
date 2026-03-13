using System.ComponentModel.DataAnnotations;

namespace ClinicBookingSystem.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int? ReceiverId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
        [StringLength(1000)]
        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        [Display(Name = "Phản hồi từ Admin")]
        public string? AdminReply { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Pending"; // Pending, Replied

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? RepliedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public User? Receiver { get; set; }
    }
}