using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using hastane.Models;
using hastane.Data;
using System.Linq;
using System.Threading.Tasks;

namespace hastane.Controllers
{
    // Doktor yetkisi kontrolü için özel filtre
    public class AuthorizeDoctorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var userRole = httpContext.Session.GetString("UserRole");
            var doctorId = httpContext.Session.GetInt32("DoctorId");
            
            // Doktor değilse, giriş sayfasına yönlendir
            if (userRole != "Doctor" || !doctorId.HasValue)
            {
                context.Result = new RedirectToActionResult("DoctorLogin", "Account", null);
                return;
            }
            
            // Doktor, Dashboard dışında bir action'a erişmeye çalışıyorsa, Dashboard'a yönlendir
            var controllerName = context.RouteData.Values["controller"].ToString();
            var actionName = context.RouteData.Values["action"].ToString();
            
            if (controllerName == "Doctor" && actionName != "Dashboard")
            {
                context.Result = new RedirectToActionResult("Dashboard", "Doctor", null);
            }
            
            base.OnActionExecuting(context);
        }
    }

    [AuthorizeDoctor]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Doktor dashboard sayfası
        public async Task<IActionResult> Dashboard(string activeTab = null)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var doctorName = HttpContext.Session.GetString("DoctorName");

            try
            {
                // Aktif tab kontrolü (dashboard varsayılan)
                ViewBag.ActiveTab = activeTab ?? "dashboard";
                
                // Doktora ait bilgileri getir
                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doktor bilgileri bulunamadı.";
                    return RedirectToAction("DoctorLogin", "Account");
                }
                
                ViewBag.Doctor = doctor;
                
                // Tüm randevuları getir (DoctorAppointmentsViewModel için)
                var allAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();
                
                // Bugün ve gelecek randevular için (DoctorDashboardViewModel için)
                var today = DateTime.Today;
                var currentAndFutureAppointments = allAppointments
                    .Where(a => a.AppointmentDateTime >= today && !a.IsCancelled)
                    .OrderBy(a => a.AppointmentDateTime)
                    .ToList();

                // Dashboard için özet sayılar
                ViewBag.TodayAppointmentsCount = allAppointments.Count(a => a.AppointmentDateTime.Date == today && !a.IsCancelled && !a.IsCompleted);
                ViewBag.UpcomingAppointmentsCount = allAppointments.Count(a => a.AppointmentDateTime.Date > today && !a.IsCancelled && !a.IsCompleted);
                
                // Appointments sekmesi için tüm randevu türlerini gruplandır
                var completedAppointments = allAppointments.Where(a => a.IsCompleted).ToList();
                var appointmentsViewModel = new DoctorAppointmentsViewModel
                {
                    UpcomingAppointments = allAppointments.Where(a => a.AppointmentDateTime.Date > today && !a.IsCancelled && !a.IsCompleted).ToList(),
                    TodayAppointments = allAppointments.Where(a => a.AppointmentDateTime.Date == today && !a.IsCancelled && !a.IsCompleted).ToList(),
                    PastAppointments = allAppointments.Where(a => a.AppointmentDateTime.Date < today && !a.IsCancelled && !a.IsCompleted).ToList(),
                    CancelledAppointments = allAppointments.Where(a => a.IsCancelled).ToList(),
                    CompletedAppointments = completedAppointments
                };
                
                // Her iki model de ViewBag üzerinden erişilebilir
                ViewBag.AppointmentsViewModel = appointmentsViewModel;
                
                // Dashboard ana modeli
                var dashboardViewModel = new DoctorDashboardViewModel
                {
                    TodayAppointments = appointmentsViewModel.TodayAppointments,
                    UpcomingAppointments = appointmentsViewModel.UpcomingAppointments
                };

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevular yüklenirken bir hata oluştu: " + ex.Message;
                return View(new DoctorDashboardViewModel());
            }
        }

        // Tüm randevuları listele - AuthorizeDoctor filtresi tarafından engellenir
        public async Task<IActionResult> Appointments(string activeTab = null)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            try
            {
                // Aktif tab kontrolü
                ViewBag.ActiveTab = activeTab ?? (TempData["ActiveTab"]?.ToString() ?? "today");
                
                // Doktorun tüm randevularını getir
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();

                // Tamamlanan randevuları test için log kayıtları oluştur
                var completedAppointments = appointments.Where(a => a.IsCompleted).ToList();
                System.Diagnostics.Debug.WriteLine($"Tamamlanan randevu sayısı: {completedAppointments.Count}");
                foreach (var app in completedAppointments)
                {
                    System.Diagnostics.Debug.WriteLine($"Tamamlanan randevu: ID: {app.Id}, Hasta: {app.Patient?.FullName}, Tarih: {app.AppointmentDateTime}, IsCompleted: {app.IsCompleted}");
                }
                
                // Test için tamamlanan randevu oluştur
                if (completedAppointments.Count == 0 && appointments.Count > 0)
                {
                    TempData["InfoMessage"] = "Tamamlanan randevu bulunamadı. Test için en az bir randevuyu manuel olarak tamamlamanız gerekiyor.";
                }

                // Randevuları gruplandır: Gelecek, Bugün, Geçmiş, İptal Edilenler
                var today = DateTime.Today;
                var viewModel = new DoctorAppointmentsViewModel
                {
                    UpcomingAppointments = appointments.Where(a => a.AppointmentDateTime.Date > today && !a.IsCancelled && !a.IsCompleted).ToList(),
                    TodayAppointments = appointments.Where(a => a.AppointmentDateTime.Date == today && !a.IsCancelled && !a.IsCompleted).ToList(),
                    PastAppointments = appointments.Where(a => a.AppointmentDateTime.Date < today && !a.IsCancelled && !a.IsCompleted).ToList(),
                    CancelledAppointments = appointments.Where(a => a.IsCancelled).ToList(),
                    CompletedAppointments = completedAppointments
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevular yüklenirken bir hata oluştu: " + ex.Message;
                return View(new DoctorAppointmentsViewModel());
            }
        }

        // Randevu detayları - AuthorizeDoctor filtresi tarafından engellenir
        public async Task<IActionResult> AppointmentDetails(int id)
        {
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

        // Randevu tamamlandı olarak işaretle - AuthorizeDoctor filtresi tarafından engellenir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int appointmentId, string notes)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.DoctorId == doctorId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Randevu bulunamadı.";
                    return RedirectToAction("Appointments");
                }

                appointment.IsCompleted = true;
                appointment.Notes = notes;
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Randevu başarıyla tamamlandı olarak işaretlendi. Notlar hasta ile paylaşıldı.";
                
                // Doğrudan CompletedAppointments sayfasına yönlendir
                return RedirectToAction("CompletedAppointments");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevu güncellenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Appointments");
            }
        }

        // Test: Sadece tamamlanan randevuları görüntüle - AuthorizeDoctor filtresi tarafından engellenir
        public async Task<IActionResult> CompletedAppointments()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            try
            {
                // Sadece tamamlanan randevuları getir
                var completedAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId && a.IsCompleted)
                    .OrderByDescending(a => a.AppointmentDateTime)
                    .ToListAsync();

                ViewBag.CompletedCount = completedAppointments.Count;

                return View(completedAppointments);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Randevular yüklenirken bir hata oluştu: " + ex.Message;
                return View(new List<Appointment>());
            }
        }
    }
} 