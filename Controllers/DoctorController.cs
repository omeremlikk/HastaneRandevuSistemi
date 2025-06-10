using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using hastane.Models;
using hastane.Data;
using System.Linq;
using System.Threading.Tasks;

namespace hastane.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Doktor yetkisi kontrolü için özel metot
        private bool IsDoctorAuthorized()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var userRole = HttpContext.Session.GetString("UserRole");
            return doctorId.HasValue && userRole == "Doctor";
        }

        // Doktor dashboard sayfası
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDoctorAuthorized())
            {
                return RedirectToAction("DoctorLogin", "Account");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var doctorName = HttpContext.Session.GetString("DoctorName");

            try
            {
                // Doktora ait bilgileri getir
                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doktor bilgileri bulunamadı.";
                    return RedirectToAction("DoctorLogin", "Account");
                }

                // Doktorun randevularını getir (bugün ve sonrası için)
                var today = DateTime.Today;
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId && 
                                a.AppointmentDateTime >= today && 
                                !a.IsCancelled)
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToListAsync();

                ViewBag.Doctor = doctor;
                ViewBag.TodayAppointmentsCount = appointments.Count(a => a.AppointmentDateTime.Date == today);
                ViewBag.UpcomingAppointmentsCount = appointments.Count(a => a.AppointmentDateTime.Date > today);
                
                // Bugünün randevularını ve gelecek randevuları ayrı grupla
                var viewModel = new DoctorDashboardViewModel
                {
                    TodayAppointments = appointments.Where(a => a.AppointmentDateTime.Date == today).ToList(),
                    UpcomingAppointments = appointments.Where(a => a.AppointmentDateTime.Date > today).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevular yüklenirken bir hata oluştu: " + ex.Message;
                return View(new DoctorDashboardViewModel());
            }
        }

        // Tüm randevuları listele
        public async Task<IActionResult> Appointments()
        {
            if (!IsDoctorAuthorized())
            {
                return RedirectToAction("DoctorLogin", "Account");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            try
            {
                // Doktorun tüm randevularını getir
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();

                // Randevuları gruplandır: Gelecek, Bugün, Geçmiş, İptal Edilenler
                var today = DateTime.Today;
                var viewModel = new DoctorAppointmentsViewModel
                {
                    UpcomingAppointments = appointments.Where(a => a.AppointmentDateTime.Date > today && !a.IsCancelled).ToList(),
                    TodayAppointments = appointments.Where(a => a.AppointmentDateTime.Date == today && !a.IsCancelled).ToList(),
                    PastAppointments = appointments.Where(a => a.AppointmentDateTime.Date < today && !a.IsCancelled).ToList(),
                    CancelledAppointments = appointments.Where(a => a.IsCancelled).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevular yüklenirken bir hata oluştu: " + ex.Message;
                return View(new DoctorAppointmentsViewModel());
            }
        }

        // Randevu detayları
        public async Task<IActionResult> AppointmentDetails(int id)
        {
            if (!IsDoctorAuthorized())
            {
                return RedirectToAction("DoctorLogin", "Account");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            try
            {
                // Randevu detaylarını getir
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Randevu bulunamadı.";
                    return RedirectToAction("Appointments");
                }

                return View(appointment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevu detayları yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Appointments");
            }
        }

        // Randevu tamamlandı olarak işaretle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int appointmentId, string notes)
        {
            if (!IsDoctorAuthorized())
            {
                return RedirectToAction("DoctorLogin", "Account");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctorId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Randevu bulunamadı.";
                    return RedirectToAction("Appointments");
                }

                appointment.IsCompleted = true;
                appointment.Notes = notes;
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Randevu başarıyla tamamlandı olarak işaretlendi.";
                return RedirectToAction("Appointments");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevu güncellenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Appointments");
            }
        }
    }
} 