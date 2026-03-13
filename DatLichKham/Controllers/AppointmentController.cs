using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClinicBookingSystem.Models;
using DatLichKham.Services;

namespace ClinicBookingSystem.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IDoctorService _doctorService;

        public AppointmentController(IAppointmentService appointmentService, IDoctorService doctorService)
        {
            _appointmentService = appointmentService;
            _doctorService = doctorService;
        }

        // GET: Appointment/MyAppointments
        public async Task<IActionResult> MyAppointments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var appointments = await _appointmentService.GetByUserIdAsync(userId.Value);
            return View(appointments);
        }

        // GET: Appointment/Create
        public async Task<IActionResult> Create(int? doctorId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var doctors = await _doctorService.GetAllAsync();
            var active = doctors.Where(d => d.IsActive).ToList();
            ViewBag.Doctors = new SelectList(active, "Id", "FullName", doctorId);

            var appointment = new Appointment();
            if (doctorId.HasValue)
            {
                appointment.DoctorId = doctorId.Value;
            }

            return View(appointment);
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Basic client-side-like validation preserved here for UX
            if (appointment.AppointmentDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("AppointmentDate", "Ngày khám phải từ hôm nay trở đi");
            }

            if (!ModelState.IsValid)
            {
                var doctors = await _doctorService.GetAllAsync();
                ViewBag.Doctors = new SelectList(doctors.Where(d => d.IsActive), "Id", "FullName", appointment.DoctorId);
                return View(appointment);
            }

            var (success, error) = await _appointmentService.CreateAsync(appointment, userId.Value);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Đặt lịch thất bại.");
                var doctors = await _doctorService.GetAllAsync();
                ViewBag.Doctors = new SelectList(doctors.Where(d => d.IsActive), "Id", "FullName", appointment.DoctorId);
                return View(appointment);
            }

            TempData["SuccessMessage"] = "Đặt lịch khám thành công! Vui lòng chờ xác nhận.";
            return RedirectToAction(nameof(MyAppointments));
        }

        // GET: Appointment/Cancel/5
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var appointment = await _appointmentService.GetByIdForUserAsync(id.Value, userId.Value);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointment/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var (success, error) = await _appointmentService.CancelAsync(id, userId.Value);
            if (success)
            {
                TempData["SuccessMessage"] = "Hủy lịch khám thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = error ?? "Không thể hủy lịch.";
            }

            return RedirectToAction(nameof(MyAppointments));
        }
    }
}