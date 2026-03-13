using DatLichKham.Services;
using Microsoft.AspNetCore.Mvc;


namespace ClinicBookingSystem.Controllers
{
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;
        private readonly IMessageService _messageService;

        public DoctorController(
            IDoctorService doctorService,
            IAppointmentService appointmentService,
            IMessageService messageService)
        {
            _doctorService = doctorService;
            _appointmentService = appointmentService;
            _messageService = messageService;
        }

        // Trang chủ của Bác sĩ: Xem lịch hẹn khách đã đặt
        public async Task<IActionResult> Index()
        {
            var sessUserId = HttpContext.Session.GetInt32("UserId");
            if (sessUserId == null) return RedirectToAction("Login", "Account");

            var doctor = await _doctorService.GetByUserIdAsync(sessUserId.Value);
            if (doctor is null) return Content("Tài khoản này chưa được cấu hình là bác sĩ.");

            var myAppointments = await _appointmentService.GetByDoctorIdAsync(doctor.Id);
            return View(myAppointments);
        }

        public async Task<IActionResult> MySchedule()
        {
            var sessUserId = HttpContext.Session.GetInt32("UserId");
            if (sessUserId == null) return RedirectToAction("Login", "Account");

            var doctor = await _doctorService.GetByUserIdAsync(sessUserId.Value);
            if (doctor is null) return NotFound();

            var data = await _appointmentService.GetByDoctorIdAsync(doctor.Id);
            return View(data);
        }

        // /Doctor/PatientMessages
        //public async Task<IActionResult> PatientMessages()
        //{
        //    var sessUserId = HttpContext.Session.GetInt32("UserId");
        //    if (sessUserId == null) return RedirectToAction("Login", "Account");

        //    var doctor = await _doctorService.GetByUserIdAsync(sessUserId.Value);
        //    if (doctor is null) return NotFound();

        //    var messages = await _messageService.GetByReceiverIdAsync(sessUserId.Value);
        //    return View(messages);
        //}
        // Doctor confirms an appointment belonging to them
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int id, string status)
        {
            var sessUserId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role") ?? "";

            if (sessUserId == null)
                return RedirectToAction("Login", "Account");

            var (success, error) = await _appointmentService.UpdateStatusAsync(
                id,
                status,
                sessUserId.Value,
                role
            );

            if (!success)
                TempData["ErrorMessage"] = error ?? "Không thể cập nhật trạng thái";
            else
                TempData["SuccessMessage"] = "Cập nhật trạng thái thành công";

            return RedirectToAction(nameof(MySchedule));
        }
    }
}