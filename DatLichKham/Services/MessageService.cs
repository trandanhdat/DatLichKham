using ClinicBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DatLichKham.Services
{
    public interface IMessageService
    {
        Task<List<Message>> GetAllAsync();
        Task<List<Message>> GetByUserIdAsync(int userId);
        Task<List<Message>> GetByReceiverIdAsync(int receiverId);
        Task<Message?> GetByIdAsync(int id);
        Task<Message> CreateAsync(Message message);
        Task<(bool success, string? error)> ReplyAsync(int messageId, int replierUserId, string replyContent);

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

            if (message.ReceiverId != replierUserId)
                return (false, "Bạn không có quyền trả lời tin nhắn này.");

            message.AdminReply = replyContent;
            message.Status = "Replied";
            message.RepliedAt = DateTime.Now;

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