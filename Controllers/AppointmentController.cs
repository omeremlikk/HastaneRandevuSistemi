using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hastane.Models;
using hastane.Data;
using System.Globalization;

namespace hastane.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Doctors()
        {
            var doctors = _context.Doctors.ToList();
            return View(doctors);
        }

        public IActionResult Departments()
        {
            var departments = _context.Departments.ToList();
            return View(departments);
        }

        public IActionResult Randevu()
        {
            var doctors = _context.Doctors.ToList();
            return View(doctors);
        }

        public async Task<IActionResult> Book(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                return NotFound();
            }

            var viewModel = new AppointmentViewModel
            {
                Doctor = doctor,
                Appointment = new Appointment { DoctorId = doctorId }
            };

            return View(viewModel);
        }

        [HttpGet]
        public JsonResult GetAvailableSlots(int doctorId, DateTime date)
        {
            var doctor = _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefault(d => d.Id == doctorId);

            if (doctor == null)
            {
                return Json(new List<object>());
            }

            var availableSlots = doctor.GetAvailableTimeSlots(date);
            
            return Json(availableSlots.Select(time => new { 
                time = time, 
                available = true 
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] AppointmentViewModel model)
        {
            if (model.Appointment == null || model.Appointment.DoctorId == 0 || model.Appointment.AppointmentDateTime == default)
            {
                return Json(new { success = false, message = "Lütfen tarih ve saat seçiniz." });
            }

            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == model.Appointment.DoctorId);

            if (doctor == null)
            {
                return Json(new { success = false, message = "Doktor bulunamadı." });
            }

            // Seçilen tarih ve saatte randevu var mı kontrol et
            var appointmentDate = model.Appointment.AppointmentDateTime.Date;
            var appointmentTime = model.Appointment.AppointmentDateTime.ToString("HH:mm");

            if (!doctor.IsAvailable(appointmentDate, appointmentTime))
            {
                return Json(new { success = false, message = "Bu tarih ve saatte randevu dolu." });
            }

            try
            {
                _context.Add(model.Appointment);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Randevu başarıyla oluşturuldu." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Randevu oluşturulurken bir hata oluştu: " + ex.Message });
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult ThankYou()
        {
            return View();
        }

        public IActionResult DoctorDetails(int id)
        {
            var doctor = _context.Doctors.Find(id);
            
            if (doctor == null)
            {
                return NotFound();
            }
            
            return View(doctor);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableDays(int doctorId, DateTime startDate)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                return NotFound();
            }

            var days = new List<object>();
            var currentDate = startDate.Date;
            
            // 30 günlük takvimi oluştur
            for (int i = 0; i < 30; i++)
            {
                var date = currentDate.AddDays(i);
                var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                var isPastDate = date.Date < DateTime.Today;

                // O gün için randevuları kontrol et
                var availableSlots = doctor.GetAvailableTimeSlots(date);
                var isAvailable = !isWeekend && !isPastDate && availableSlots.Any();

                days.Add(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    dayOfWeek = date.ToString("dddd", new System.Globalization.CultureInfo("tr-TR")),
                    day = date.Day,
                    month = date.ToString("MMMM", new System.Globalization.CultureInfo("tr-TR")),
                    available = isAvailable,
                    isWeekend = isWeekend,
                    isPastDate = isPastDate
                });
            }

            return Json(days);
        }

        [HttpGet]
        public async Task<JsonResult> CheckAvailableSlots(int doctorId, DateTime date)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
            {
                return Json(new List<object>());
            }

            // Tüm saat aralıkları
            var allTimeSlots = new List<string> {
                "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30"
            };

            // Her saat için müsaitlik durumunu kontrol et
            var availableSlots = allTimeSlots.Select(time => new {
                time = time,
                isAvailable = doctor.IsAvailable(date, time)
            }).ToList();

            return Json(availableSlots);
        }
    }
}
