using Microsoft.AspNetCore.Mvc;
using ClinicBookingSystem.Models;
using DatLichKham.Services;

namespace ClinicBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly IAccountService _accountService;

        public HomeController(IDoctorService doctorService, IAccountService accountService)
        {
            _doctorService = doctorService;
            _accountService = accountService;
        }

        public IActionResult Index()
        {
            var id = HttpContext.Session.GetInt32("UserId");
            var user = _accountService.GetByIdAsync(id ?? 0).Result;
            ViewBag.UserId = id;
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Role = HttpContext.Session.GetString("Role");
            return View(user);
        }

        public async Task<IActionResult> Doctors(string searchString, int? pageNumber)
        {
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");

            // Preserve search for view
            ViewData["CurrentFilter"] = searchString;

            int pageSize = 6;
            var paged = await _doctorService.GetPagedAsync(searchString, pageNumber ?? 1, pageSize);

            return View(paged);
        }

        public async Task<IActionResult> DoctorDetails(int id)
        {
            var doctor = await _doctorService.GetByIdWithUserAsync(id);
            if (doctor == null) return NotFound();

            return View(doctor);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}