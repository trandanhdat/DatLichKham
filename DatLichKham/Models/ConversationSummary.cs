namespace DatLichKham.Models
{
    public class ConversationSummary
    {

        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public bool IsDoctor { get; set; }
        public string? DoctorSpecialty { get; set; }
    }
}
