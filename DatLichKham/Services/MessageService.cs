using ClinicBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DatLichKham.Services
{
    public interface IMessageService
    {
        Task<List<Message>> GetAllAsync();
        // Lấy tất cả tin nhắn của một user (bệnh nhân xem tin của mình)
        Task<List<Message>> GetByUserIdAsync(int userId);

        // Lấy tất cả tin nhắn gửi đến một receiver (bác sĩ xem inbox)
        Task<List<Message>> GetByReceiverIdAsync(int receiverId);

        // Lấy một tin nhắn theo id
        Task<Message?> GetByIdAsync(int id);

        // Tạo tin nhắn mới, trả về entity đã lưu (có Id)
        Task<Message> CreateAsync(Message message);

        // Bác sĩ / admin trả lời tin nhắn
        // Trả về (success, errorMessage)
        Task<(bool success, string? error)> ReplyAsync(int messageId, int replierUserId, string replyContent);

        // Lấy toàn bộ lịch sử hội thoại giữa 2 user (dùng cho ChatWithDoctor)
        Task<List<Message>> GetConversationAsync(int userId, int otherUserId);
    }

    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }
        // ── Lấy tất cả tin nhắn (Admin) ──────────────────────────────────────
        public async Task<List<Message>> GetAllAsync()
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        // ── Lấy tất cả tin nhắn bệnh nhân đã gửi ─────────────────────────────
        public async Task<List<Message>> GetByUserIdAsync(int userId)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Receiver)
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        // ── Lấy inbox của bác sĩ (tin nhắn gửi đến họ) ───────────────────────
        public async Task<List<Message>> GetByReceiverIdAsync(int receiverId)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == receiverId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        // ── Lấy một tin nhắn theo Id ──────────────────────────────────────────
        public async Task<Message?> GetByIdAsync(int id)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // ── Tạo tin nhắn mới ──────────────────────────────────────────────────
        public async Task<Message> CreateAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message; // EF đã gán Id sau SaveChanges
        }

        // ── Bác sĩ / admin trả lời tin nhắn ──────────────────────────────────
        public async Task<(bool success, string? error)> ReplyAsync(
            int messageId, int replierUserId, string replyContent)
        {
            if (string.IsNullOrWhiteSpace(replyContent))
                return (false, "Nội dung trả lời không được để trống.");

            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
                return (false, "Tin nhắn không tồn tại.");

            // Kiểm tra quyền: chỉ receiver mới được trả lời
            if (message.ReceiverId != replierUserId)
                return (false, "Bạn không có quyền trả lời tin nhắn này.");

            // Cập nhật tin nhắn gốc
            message.AdminReply = replyContent;
            message.Status = "Replied";
            message.RepliedAt = DateTime.Now;

            // Tạo tin nhắn phản hồi mới để hiển thị trong conversation
            var replyMsg = new Message
            {
                UserId = replierUserId,
                ReceiverId = message.UserId,
                Content = replyContent,
                Status = "Sent",
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(replyMsg);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        // ── Lấy lịch sử hội thoại giữa 2 người ──────────────────────────────
        public async Task<List<Message>> GetConversationAsync(int userId, int otherUserId)
        {
            return await _context.Messages
                .Include(m => m.User)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.UserId == userId && m.ReceiverId == otherUserId) ||
                    (m.UserId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}