using ClinicBookingSystem.Models;
using DatLichKham.Hubs;
using DatLichKham.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;

namespace ClinicBookingSystem.Controllers
{
    public class MessageController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly IAccountService _accountService;
        private readonly IHubContext<ChatHub> _chatHub;

        public MessageController(
            IMessageService messageService,
            IAccountService accountService,
            IHubContext<ChatHub> chatHub)
        {
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _chatHub = chatHub ?? throw new ArgumentNullException(nameof(chatHub));
        }

        // ── MyMessages ────────────────────────────────────────────────────────
        public async Task<IActionResult> MyMessages()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var messages = await _messageService.GetByUserIdAsync(userId.Value);
            return View(messages);
        }

        // ── Create (GET) ──────────────────────────────────────────────────────
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            return View();
        }

        // ── Create (POST) ─────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Message message)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                message.UserId = userId.Value;
                message.Status = "Pending";
                message.CreatedAt = DateTime.Now;

                await _messageService.CreateAsync(message);

                TempData["SuccessMessage"] = "Gửi tin nhắn thành công!";
                return RedirectToAction(nameof(MyMessages));
            }

            return View(message);
        }

        // ── DoctorMessages ────────────────────────────────────────────────────
        public async Task<IActionResult> DoctorMessages()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var messages = await _messageService.GetByReceiverIdAsync(userId.Value);
            return View(messages);
        }

        // ── ReplyMessage (GET) ────────────────────────────────────────────────
        public async Task<IActionResult> ReplyMessage(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var message = await _messageService.GetByIdAsync(id);
            if (message == null) return NotFound();

            return View(message);
        }

        // ── ReplyMessage (POST) ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int id, string adminReply)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var (success, error) = await _messageService.ReplyAsync(id, userId.Value, adminReply);

            if (!success)
                TempData["ErrorMessage"] = error ?? "Không thể phản hồi.";
            else
                TempData["SuccessMessage"] = "Phản hồi đã gửi.";

            return RedirectToAction(nameof(DoctorMessages));
        }

        // ── ChatWithDoctor (GET) ──────────────────────────────────────────────
        public async Task<IActionResult> ChatWithDoctor(int receiverId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var chatHistory = await _messageService.GetConversationAsync(userId.Value, receiverId);

            var receiver = await _accountService.GetByIdAsync(receiverId);
            ViewBag.ReceiverId = receiverId;
            ViewBag.ReceiverName = receiver?.FullName;
            ViewBag.CurrentUserId = userId.Value;

            return View(chatHistory);
        }

        // ── SendChatMessage (POST) ────────────────────────────────────────────
        // Gửi tin nhắn VÀ push SignalR đến người nhận ngay lập tức
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendChatMessage(int receiverId, string content)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;

            if (userId == null || string.IsNullOrWhiteSpace(content))
                return BadRequest();

            var receiverUser = await _accountService.GetByIdAsync(receiverId);
            if (receiverUser == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, error = "Người nhận không tồn tại." });
                TempData["ErrorMessage"] = "Người nhận không tồn tại.";
                return RedirectToAction("ChatWithDoctor", new { receiverId });
            }

            // Bác sĩ không nhắn cho bác sĩ khác
            if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
             && string.Equals(receiverUser.Role, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, error = "Bác sĩ không thể nhắn tin cho bác sĩ khác." });
                TempData["ErrorMessage"] = "Bác sĩ không thể nhắn tin cho bác sĩ khác.";
                return RedirectToAction("DoctorMessages");
            }

            // Lưu DB
            var msg = new Message
            {
                UserId = userId.Value,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.Now,
                Status = "Sent"
            };
            var created = await _messageService.CreateAsync(msg);

            // ── PUSH SignalR đến người nhận ──────────────────────────────────
            await _chatHub.Clients
                .Group($"user_{receiverId}")
                .SendAsync("ReceiveMessage", new
                {
                    id = created.Id,
                    content = created.Content,
                    senderId = created.UserId,
                    createdAt = created.CreatedAt.ToString("HH:mm"),
                    dateStr = created.CreatedAt.ToString("dd/MM/yyyy"),
                    isToday = created.CreatedAt.Date == DateTime.Today
                });

            // AJAX response
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    id = created.Id,
                    content = created.Content,
                    senderId = created.UserId,
                    createdAt = created.CreatedAt.ToString("HH:mm"),
                    dateStr = created.CreatedAt.ToString("dd/MM/yyyy"),
                    isToday = created.CreatedAt.Date == DateTime.Today
                });
            }

            return RedirectToAction("ChatWithDoctor", new { receiverId });
        }
    }
}