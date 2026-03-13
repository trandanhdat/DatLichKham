using ClinicBookingSystem.Models;
using DatLichKham.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicBookingSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;
        private readonly IMessageService _messageService;

        public AdminController(
            IAccountService accountService,
            IDoctorService doctorService,
            IAppointmentService appointmentService,
            IMessageService messageService)
        {
            _accountService = accountService;
            _doctorService = doctorService;
            _appointmentService = appointmentService;
            _messageService = messageService;
        }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("Role") == "Admin";

        // ── Dashboard ─────────────────────────────────────────────────────────
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            return View();
        }

        // ═════════════════════════════════════════════════════════════════════
        // QUẢN LÝ TÀI KHOẢN
        // ═════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Accounts()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _accountService.GetAllAsync());
        }

        public async Task<IActionResult> CreateAccount()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var doctors = await _doctorService.GetAllAsync();
            ViewBag.Doctors = new SelectList(
                doctors.Where(d => d.UserId == null), "Id", "FullName");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(
            User user, string password, string confirmPassword, int? doctorId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            if (password != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");

            if (!ModelState.IsValid)
            {
                var doctors = await _doctorService.GetAllAsync();
                ViewBag.Doctors = new SelectList(
                    doctors.Where(d => d.UserId == null), "Id", "FullName", doctorId);
                return View(user);
            }

            var (success, error) = await _accountService.CreateAccountAsync(user, password);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Tạo tài khoản thất bại.");
                var doctors = await _doctorService.GetAllAsync();
                ViewBag.Doctors = new SelectList(
                    doctors.Where(d => d.UserId == null), "Id", "FullName", doctorId);
                return View(user);
            }

            if (doctorId.HasValue)
            {
                var created = (await _accountService.GetAllAsync())
                    .FirstOrDefault(u => u.Username == user.Username);
                if (created != null)
                {
                    var doctor = await _doctorService.GetByIdAsync(doctorId.Value);
                    if (doctor != null)
                    {
                        doctor.UserId = created.Id;
                        await _doctorService.UpdateAsync(doctor);
                    }
                }
            }

            TempData["SuccessMessage"] = "Tạo tài khoản thành công!";
            return RedirectToAction(nameof(Accounts));
        }

        public async Task<IActionResult> EditAccount(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var user = await _accountService.GetByIdAsync(id.Value);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(
            int id, User updatedUser, string newPassword, string confirmPassword)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id != updatedUser.Id) return NotFound();

            if (!string.IsNullOrEmpty(newPassword) && newPassword != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");

            if (!ModelState.IsValid) return View(updatedUser);

            var (success, error) = await _accountService.UpdateAccountAsync(
                updatedUser,
                string.IsNullOrWhiteSpace(newPassword) ? null : newPassword);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Cập nhật thất bại.");
                return View(updatedUser);
            }

            TempData["SuccessMessage"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Accounts));
        }

        public async Task<IActionResult> DeleteAccount(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var user = await _accountService.GetByIdAsync(id.Value);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteAccount"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var deleted = await _accountService.DeleteAccountAsync(id);
            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] =
                deleted ? "Xóa tài khoản thành công!" : "Xóa tài khoản thất bại.";
            return RedirectToAction(nameof(Accounts));
        }

        // ═════════════════════════════════════════════════════════════════════
        // QUẢN LÝ BÁC SĨ
        // ═════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Doctors()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _doctorService.GetAllAsync());
        }

        public IActionResult CreateDoctor()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoctor(
            Doctor doctor,
            bool createAccount = false,
            string? username = null,
            string? password = null,
            string? confirmPassword = null)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid) return View(doctor);

            var created = await _doctorService.CreateAsync(doctor);

            if (createAccount)
            {
                if (string.IsNullOrWhiteSpace(username))
                    ModelState.AddModelError("Username", "Vui lòng nhập tên đăng nhập.");
                if (string.IsNullOrWhiteSpace(password))
                    ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
                if (password != confirmPassword)
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                if (await _accountService.UsernameExistsAsync(username ?? string.Empty))
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");

                if (!ModelState.IsValid) return View(created);

                var (ok, err) = await _accountService.CreateDoctorAccountAsync(
                    created.Id, username!, password!);
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, err ?? "Tạo tài khoản bác sĩ thất bại.");
                    return View(created);
                }
            }

            TempData["SuccessMessage"] = "Thêm bác sĩ thành công!";
            return RedirectToAction(nameof(Doctors));
        }

        public async Task<IActionResult> EditDoctor(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var doctor = await _doctorService.GetByIdAsync(id.Value);
            if (doctor == null) return NotFound();
            var users = await _accountService.GetAllAsync();
            ViewBag.Users = new SelectList(users, "Id", "Username", doctor.UserId);
            return View(doctor);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(int id, Doctor doctor, int? selectedUserId,
            bool createAccount = false, string? username = null, string? password = null, string? confirmPassword = null)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id != doctor.Id) return NotFound();

            // 1. Kiểm tra Validate cơ bản
            if (!ModelState.IsValid) goto ReturnView;

            // 2. Xử lý Tạo tài khoản mới
            if (createAccount)
            {

                var (ok, errOrUserId) = await _accountService.CreateDoctorAccountAsync(doctor.Id, username!, password!);

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, errOrUserId ?? "Thất bại");
                    goto ReturnView;
                }

                if (int.TryParse(errOrUserId, out int newId))
                {
                    doctor.UserId = newId;
                }
            }
            // 3. Hoặc xử lý Gắn tài khoản có sẵn
            else if (selectedUserId.HasValue)
            {
                doctor.UserId = selectedUserId;
            }

            // 4. Cập nhật cuối cùng (Chỉ gọi 1 lần duy nhất ở cuối)
            if (await _doctorService.UpdateAsync(doctor))
            {
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Doctors));
            }

            return NotFound();

        // Nhãn hỗ trợ trả về View khi có lỗi để tránh lặp code SelectList
        ReturnView:
            ViewBag.Users = new SelectList(await _accountService.GetAllAsync(), "Id", "Username", doctor.UserId);
            return View(doctor);
        }


        public async Task<IActionResult> DeleteDoctor(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var doctor = await _doctorService.GetByIdAsync(id.Value);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        [HttpPost, ActionName("DeleteDoctor"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var success = await _doctorService.SoftDeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Vô hiệu hóa bác sĩ thành công!" : "Không thể vô hiệu hóa.";
            return RedirectToAction(nameof(Doctors));
        }

        // ═════════════════════════════════════════════════════════════════════
        // QUẢN LÝ LỊCH KHÁM
        // ═════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Appointments()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _appointmentService.GetAllAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var updated = await _appointmentService.UpdateStatusAsync(id, status);
            TempData[updated ? "SuccessMessage" : "ErrorMessage"] =
                updated ? "Cập nhật trạng thái thành công!" : "Cập nhật thất bại.";
            return RedirectToAction(nameof(Appointments));
        }

        // ═════════════════════════════════════════════════════════════════════
        // QUẢN LÝ TIN NHẮN
        // ═════════════════════════════════════════════════════════════════════

        public async Task<IActionResult> Messages()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View(await _messageService.GetAllAsync());
        }

        public async Task<IActionResult> ReplyMessage(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (id == null) return NotFound();
            var message = await _messageService.GetByIdAsync(id.Value);
            if (message == null) return NotFound();
            return View(message);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int id, string adminReply)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var adminUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var (success, error) = await _messageService.ReplyAsync(id, adminUserId, adminReply);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? "Phản hồi đã gửi thành công!" : (error ?? "Không thể phản hồi.");
            return RedirectToAction(nameof(Messages));
        }
    }
}