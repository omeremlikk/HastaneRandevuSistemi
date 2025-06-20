using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hastane.Models;
using hastane.Data;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace hastane.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tüm controller metotları için çalışacak özel action filtresi
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Eğer kullanıcı doktor rolündeyse, doktor dashboard'una yönlendir
            if (HttpContext.Session.GetString("UserRole") == "Doctor")
            {
                context.Result = new RedirectToActionResult("Dashboard", "Doctor", null);
            }
        }
        
        [HttpGet]
        public JsonResult GetDebugInfo()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            var patientName = HttpContext.Session.GetString("PatientName");
            
            var debugData = new
            {
                SessionInfo = new 
                {
                    PatientId = patientId,
                    PatientName = patientName,
                    IsAuthenticated = patientId.HasValue
                },
                AppointmentsCount = 0,
                PatientExists = false,
                ServerTime = DateTime.Now
            };

            try
            {
                if (patientId.HasValue)
                {
                    // Hasta mevcut mu?
                    var patient = _context.Patients.Find(patientId.Value);
                    debugData = new
                    {
                        SessionInfo = new 
                        {
                            PatientId = patientId,
                            PatientName = patientName,
                            IsAuthenticated = patientId.HasValue
                        },
                        AppointmentsCount = _context.Appointments.Count(a => a.PatientId == patientId.Value),
                        PatientExists = patient != null,
                        ServerTime = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
            
            return Json(debugData);
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

        public async Task<IActionResult> Randevu()
        {
            try
            {
                // Bağlantıyı yenile
                _context.Database.CloseConnection();
                _context.Database.OpenConnection();
                
                // Cache'i temizle
                _context.ChangeTracker.Clear();
                
                // Doktorları yeniden yükle
            var doctors = await _context.Doctors
                .Include(d => d.Appointments)
                    .AsNoTracking()  // Tracking olmadan çek
                .ToListAsync();

            ViewBag.TimeSlots = new[] {
                "09:00", "09:30", "10:00", "10:30", "11:00",
                "11:30", "13:00", "13:30", "14:00", "14:30",
                "15:00", "15:30", "16:00", "16:30"
            };
                
                Console.WriteLine($"Randevu sayfası yüklendi: {DateTime.Now}. Yüklenen doktor sayısı: {doctors.Count}");

            return View(doctors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Randevu sayfası yükleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Sayfa yüklenirken bir hata oluştu: " + ex.Message;
                return View(new List<Doctor>());
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckDoctorAvailability(int doctorId, string date)
        {
            try
            {
                if (string.IsNullOrEmpty(date))
                {
                    return Json(new { success = false, message = "Geçerli bir tarih belirtmelisiniz." });
                }

                // Önbelleği temizle
                _context.ChangeTracker.Clear();

                DateTime selectedDate;
                try
                {
                    selectedDate = DateTime.Parse(date);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Geçersiz tarih formatı: {ex.Message}" });
                }
                
                var timeSlots = new[] {
                    "09:00", "09:30", "10:00", "10:30", "11:00",
                    "11:30", "13:00", "13:30", "14:00", "14:30",
                    "15:00", "15:30", "16:00", "16:30"
                };

                // Hafta sonu kontrolü
                bool isWeekend = selectedDate.DayOfWeek == DayOfWeek.Saturday || selectedDate.DayOfWeek == DayOfWeek.Sunday;
                
                if (isWeekend)
                {
                    Console.WriteLine($"CheckDoctorAvailability: {selectedDate.ToString("yyyy-MM-dd")} tarihi hafta sonu, tüm saatler dolu gösterilecek.");
                    
                    // Hafta sonu için tüm saatleri dolu olarak göster
                    var weekendData = timeSlots.Select(time => new {
                        time = time,
                        isAvailable = false,
                        hasAppointment = true
                    }).ToList();
                    
                    return Json(new { 
                        success = true, 
                        data = weekendData,
                        message = "Hafta sonları randevu alınamaz. Tüm saatler dolu.",
                        appointmentTimes = timeSlots.ToList()
                    });
                }

                // SQLite komutunu doğrudan çalıştır (EF Core yerine)
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var appointments = new List<(string id, int hour, int minute)>();

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var dateString = selectedDate.ToString("yyyy-MM-dd");
                    var commandText = $"SELECT Id, AppointmentDateTime FROM Appointments WHERE DoctorId = @doctorId AND date(AppointmentDateTime) = date(@dateString)";
                    
                    using (var command = new Microsoft.Data.Sqlite.SqliteCommand(commandText, connection))
                    {
                        command.Parameters.AddWithValue("@doctorId", doctorId);
                        command.Parameters.AddWithValue("@dateString", dateString);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var id = reader.GetInt32(0).ToString();
                                var dateTimeStr = reader.GetString(1);
                                var dateTime = DateTime.Parse(dateTimeStr);
                                
                                // Doğrudan tarih nesnesinden saat ve dakika bilgisini al, saat dilimi dönüşümü yapma
                                var hour = dateTime.Hour;
                                var minute = dateTime.Minute;
                                
                                appointments.Add((id, hour, minute));
                            }
                        }
                    }
                    
                    connection.Close();
                }
                
                Console.WriteLine($"Doktor {doctorId} için {selectedDate:dd.MM.yyyy} tarihinde {appointments.Count} randevu bulundu.");
                
                // Her randevu için saatini loglayalım
                foreach (var appt in appointments)
                {
                    Console.WriteLine($"- Randevu: {appt.id}, Saat: {appt.hour:00}:{appt.minute:00}");
                }
                
                // Her zaman dilimi için müsaitlik durumunu kontrol et
                var availabilityData = timeSlots.Select(time =>
                {
                    // Saati parçala
                    var parts = time.Split(':');
                    var hour = int.Parse(parts[0]);
                    var minute = int.Parse(parts[1]);
                    
                    // Bu saatte randevu var mı?
                    var hasAppointment = appointments.Any(a => a.hour == hour && a.minute == minute);
                    
                    // Final müsaitlik durumu
                    var isAvailable = !hasAppointment;
                    
                    // Randevu durumunu konsola yaz
                    Console.WriteLine($"- Saat {hour:00}:{minute:00}: {(isAvailable ? "MÜSAİT" : "DOLU")} (RandevuVarMı: {hasAppointment})");
                    
                    return new { 
                time = time,
                        isAvailable = isAvailable,
                        hasAppointment = hasAppointment
                    };
            }).ToList();

                return Json(new { 
                    success = true, 
                    data = availabilityData,
                    message = $"{appointments.Count} adet randevu bulundu",
                    appointmentTimes = appointments.Select(a => $"{a.hour:00}:{a.minute:00}").ToList()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Müsaitlik kontrolü hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Müsaitlik kontrolü sırasında hata: " + ex.Message });
            }
        }

        // Eski URL'yi yeni URL'ye yönlendirmek için
        [HttpGet]
        [Route("Appointment/Book/{doctorId:int}")]
        public IActionResult BookRedirect(int doctorId)
        {
            return RedirectToAction("BookNew", new { doctorId = doctorId });
        }
        
        // Yeni randevu oluşturma metodu
        [HttpGet]
        [Route("Randevu/Olustur/{doctorId:int}")]
        public async Task<IActionResult> BookNew(int doctorId)
        {
            // Kullanıcı giriş kontrolü
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Randevu almak için sisteme giriş yapmanız gerekmektedir.";
                return RedirectToAction("PatientLogin", "Account");
            }
            
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
                Appointment = new Appointment { 
                    DoctorId = doctorId,
                    PatientId = patientId.Value
                }
            };
            
            return View("Book", viewModel);
        }

        [HttpGet]
        public JsonResult GetAvailableSlots(int doctorId, DateTime date)
        {
            try
            {
                // Eğer gelen tarih UTC ise, yerel saate çevirmeye çalış
                if (date.Kind == DateTimeKind.Utc)
                {
                    date = date.ToLocalTime();
                    Console.WriteLine($"GetAvailableSlots: UTC tarih yerel saate çevrildi: {date}");
                }
                else
                {
                    Console.WriteLine($"GetAvailableSlots: Tarih zaten yerel saat olarak geldi: {date}");
                }

                // Tüm saat aralıkları
                var allTimeSlots = new List<string> {
                    "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                    "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30"
                };

                // Hafta sonu kontrolü
                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                if (isWeekend)
                {
                    Console.WriteLine($"GetAvailableSlots: {date.ToString("yyyy-MM-dd")} tarihi hafta sonu, tüm saatler dolu gösterilecek.");
                    
                    // Hafta sonu için tüm saatleri dolu olarak göster
                    var weekendSlots = allTimeSlots.Select(time => new {
                        time = time,
                        available = false
                    }).ToList();
                    
                    return Json(weekendSlots);
                }

                // Doktor kontrolü
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
            catch (Exception ex)
            {
                Console.WriteLine($"GetAvailableSlots hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentViewModel model)
        {
            try
            {
                // Kullanıcı giriş yapmış mı?
                if (HttpContext.Session.GetInt32("PatientId") == null)
                {
                    TempData["ErrorMessage"] = "Randevu almak için giriş yapmalısınız.";
                    return RedirectToAction("PatientLogin", "Account");
                }

                // Email ve Password validasyonunu atla
                ModelState.Clear();

                // Seçilen tarih-saat kontrolü
                if (model.Appointment.AppointmentDateTime == DateTime.MinValue)
                {
                    TempData["ErrorMessage"] = "Geçerli bir tarih ve saat seçmelisiniz.";
                    return RedirectToAction("Randevu");
                }

                // Geçmiş tarih kontrolü
                if (model.Appointment.AppointmentDateTime < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Geçmiş bir tarihe randevu alamazsınız.";
                    return RedirectToAction("Randevu");
                }

                // Hafta sonu kontrolü
                var dayOfWeek = model.Appointment.AppointmentDateTime.DayOfWeek;
                if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                {
                    TempData["ErrorMessage"] = "Hafta sonları randevu hizmeti verilmemektedir.";
                    return RedirectToAction("Randevu");
                }

                // Randevu saati kontrolü (09:00-17:00 arası, öğle arası hariç)
                var hour = model.Appointment.AppointmentDateTime.Hour;
                var minute = model.Appointment.AppointmentDateTime.Minute;
                
                if (hour < 9 || hour > 16 || (hour == 12 && minute >= 0 && minute < 30))
                {
                    TempData["ErrorMessage"] = "Randevu saatleri 09:00-12:00 ve 13:00-17:00 arasındadır.";
                    return RedirectToAction("Randevu");
                }

                // Randevu çakışması kontrolü
                var existingAppointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.DoctorId == model.Appointment.DoctorId && 
                                            a.AppointmentDateTime == model.Appointment.AppointmentDateTime);
                
                if (existingAppointment != null)
                {
                    TempData["ErrorMessage"] = "Seçtiğiniz saat için randevu dolu. Lütfen başka bir saat seçin.";
                    return RedirectToAction("Randevu");
                }

                // Randevu oluştur
                var appointment = new Appointment
                {
                    PatientId = model.Appointment.PatientId,
                    DoctorId = model.Appointment.DoctorId,
                    AppointmentDateTime = model.Appointment.AppointmentDateTime,
                    IsCompleted = false,
                    IsCancelled = false,
                    Notes = "",
                    CreatedAt = DateTime.Now
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Randevu bilgilerini TempData'ya kaydet
                TempData["LastBookedDoctorId"] = model.Appointment.DoctorId.ToString();
                TempData["LastBookedDate"] = model.Appointment.AppointmentDateTime.ToString("yyyy-MM-dd");
                TempData["LastBookedTime"] = model.Appointment.AppointmentDateTime.ToString("HH:mm");
                TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu.";

                // Success sayfasına yönlendir
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Randevu oluşturulurken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Randevu");
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult ThankYou()
        {
            // Başarılı randevu mesajı
            TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu!";
            // Yenileme gerekliliğini belirt
            TempData["RefreshNeeded"] = true;
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

                // Hafta sonları ve geçmiş tarihler müsait değil
                var isAvailable = !isPastDate && !isWeekend;

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
            try
            {
                // Eğer gelen tarih UTC ise, yerel saate çevirmeye çalış
                if (date.Kind == DateTimeKind.Utc)
                {
                    date = date.ToLocalTime();
                    Console.WriteLine($"CheckAvailableSlots: UTC tarih yerel saate çevrildi: {date}");
                }
                else
                {
                    Console.WriteLine($"CheckAvailableSlots: Tarih zaten yerel saat olarak geldi: {date}");
                }

                // Tüm saat aralıkları
                var allTimeSlots = new List<string> {
                    "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                    "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30"
                };

                // Hafta sonu kontrolü
                bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                if (isWeekend)
                {
                    Console.WriteLine($"CheckAvailableSlots: {date.ToString("yyyy-MM-dd")} tarihi hafta sonu, tüm saatler dolu gösterilecek.");
                    
                    // Hafta sonu için tüm saatleri dolu olarak göster
                    var weekendSlots = allTimeSlots.Select(time => new {
                        time = time,
                        isAvailable = false
                    }).ToList();
                    
                    return Json(weekendSlots);
                }

                // SQLite bağlantısı oluştur
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var appointments = new List<(int hour, int minute)>();

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Doktoru kontrol et
                    bool doctorExists = false;
                    using (var command = new Microsoft.Data.Sqlite.SqliteCommand("SELECT COUNT(*) FROM Doctors WHERE Id = @doctorId", connection))
                    {
                        command.Parameters.AddWithValue("@doctorId", doctorId);
                        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        doctorExists = (count > 0);
                    }
                    
                    if (!doctorExists)
                    {
                        return Json(new List<object>());
                    }
                    
                    // Randevuları getir
                    var dateString = date.ToString("yyyy-MM-dd");
                    using (var command = new Microsoft.Data.Sqlite.SqliteCommand(
                        "SELECT AppointmentDateTime FROM Appointments " +
                        "WHERE DoctorId = @doctorId AND date(AppointmentDateTime) = date(@dateString)",
                        connection))
                    {
                        command.Parameters.AddWithValue("@doctorId", doctorId);
                        command.Parameters.AddWithValue("@dateString", dateString);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var appointmentDateTimeStr = reader.GetString(0);
                                // Veritabanından okunan datetime string'ini parse ederken saat dilimi bilgisini koruyalım
                                var appointmentDateTime = DateTime.Parse(appointmentDateTimeStr);
                                Console.WriteLine($"Bulunan randevu tarihi: {appointmentDateTime} -> Parse edildi: {appointmentDateTime:dd.MM.yyyy HH:mm:ss} (Saat: {appointmentDateTime.Hour:00}:{appointmentDateTime.Minute:00})");
                                
                                // Saat ve dakikayı direkt olarak okuyalım, saat dilimi dönüşümü yapmayalım
                                appointments.Add((appointmentDateTime.Hour, appointmentDateTime.Minute));
                            }
                        }
                    }
                    
                    // Bağlantıyı kapat
                    connection.Close();
                }

                // Her saat için manuel müsaitlik kontrolü
                var availableSlots = allTimeSlots.Select(time => {
                    // Saati ayrıştır
                    var parts = time.Split(':');
                    var hour = int.Parse(parts[0]);
                    var minute = int.Parse(parts[1]);
                    
                    // Randevu var mı kontrolü
                    var matchingAppointments = appointments.Where(a => a.hour == hour && a.minute == minute).ToList();
                    var hasAppointment = matchingAppointments.Any();
                    
                    // Debug için dolu saatleri göster
                    if (hasAppointment)
                    {
                        Console.WriteLine($"** DOLU SAAT BULUNDU: {hour:00}:{minute:00} - Eşleşen randevular:");
                        foreach (var appt in appointments.Where(a => a.hour == hour && a.minute == minute))
                        {
                            Console.WriteLine($"   - ID: [Bilinmiyor], Saat: {appt.hour:00}:{appt.minute:00}");
                        }
                    }
                    
                    // Final müsaitlik durumu
                    var isAvailable = !hasAppointment;
                    
                    // Randevu durumunu konsola yaz
                    Console.WriteLine($"- Saat {hour:00}:{minute:00}: {(isAvailable ? "MÜSAİT" : "DOLU")} (RandevuVarMı: {hasAppointment})");
                    
                    return new {
                        time = time,
                        isAvailable = isAvailable
                    };
                }).ToList();

                return Json(availableSlots);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckAvailableSlots hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAppointments()
        {
            try
            {
                // SQLite bağlantısı oluştur
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // DELETE komutunu oluştur
                    using (var command = new Microsoft.Data.Sqlite.SqliteCommand("DELETE FROM Appointments", connection))
                    {
                        // Komutu çalıştır
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        // Sonuçları logla
                        Console.WriteLine($"Tüm randevular silindi. Etkilenen satır sayısı: {rowsAffected}");
                        
                        // Context'i temizle
                        _context.ChangeTracker.Clear();
                        
                        // Veritabanını yeniden başlat
                        using (var vacuumCommand = new Microsoft.Data.Sqlite.SqliteCommand("VACUUM", connection))
                        {
                            await vacuumCommand.ExecuteNonQueryAsync();
                        }
                        
                        // Başarı mesajını TempData'ya ekle
                        TempData["SuccessMessage"] = $"Tüm randevular başarıyla sıfırlandı. Silinen randevu sayısı: {rowsAffected}";
                        
                        // Bağlantıyı kapat
                        connection.Close();
                        
                        // MyAppointments sayfasına yönlendir
                        return RedirectToAction("MyAppointments");
                    }
                }
            }
            catch (Exception ex)
            {
                // Detaylı hata mesajı logla
                Console.WriteLine($"Randevu sıfırlama hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Hata mesajını TempData'ya ekle
                TempData["ErrorMessage"] = "Randevular sıfırlanırken bir hata oluştu: " + ex.Message;
                
                // Hata durumunda MyAppointments sayfasına geri dön
                return RedirectToAction("MyAppointments");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            // Kullanıcı girişi kontrolü
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin", "Account");
            }

            try
            {
                // Cache'i temizle ve veritabanı bağlantısını yenile
                _context.ChangeTracker.Clear();
                
                Console.WriteLine($"MyAppointments çağrıldı. PatientId: {patientId}");
                
                // Hastaya ait tüm randevuları direkt SQL sorgusu ile getir
                var allAppointments = new List<Appointment>();
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Hastanın tüm randevularını getir
                    var commandText = @"
                        SELECT a.Id, a.DoctorId, a.PatientId, a.AppointmentDateTime, a.CreatedAt, 
                               a.IsCancelled, a.IsCompleted, a.Notes,
                               d.Name as DoctorName, d.Specialty, d.ImageUrl, d.Description
                        FROM Appointments a
                        INNER JOIN Doctors d ON a.DoctorId = d.Id
                        WHERE a.PatientId = @patientId
                    ";
                    
                    using (var command = new Microsoft.Data.Sqlite.SqliteCommand(commandText, connection))
                    {
                        command.Parameters.AddWithValue("@patientId", patientId.Value);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Appointment nesnesi oluştur
                                var dateTimeStr = reader.GetString(3);
                                var createdAtStr = reader.GetString(4);
                                
                                Console.WriteLine($"Veritabanından okunan tarih: {dateTimeStr}");
                                
                                // Tarih ayrıştırması
                                DateTime appointmentDateTime;
                                DateTime createdAt;
                                
                                // TryParse kullanarak hata ayıklama
                                if (!DateTime.TryParse(dateTimeStr, out appointmentDateTime)) 
                                {
                                    Console.WriteLine($"HATA: Randevu tarihi ayrıştırılamadı: {dateTimeStr}");
                                    appointmentDateTime = DateTime.Now.AddYears(1); // Varsayılan gelecek tarih
                                }
                                
                                if (!DateTime.TryParse(createdAtStr, out createdAt))
                                {
                                    createdAt = DateTime.Now;
                                }
                                
                                var appointment = new Appointment
                                {
                                    Id = reader.GetInt32(0),
                                    DoctorId = reader.GetInt32(1),
                                    PatientId = reader.GetInt32(2),
                                    AppointmentDateTime = appointmentDateTime,
                                    CreatedAt = createdAt,
                                    IsCancelled = reader.GetBoolean(5),
                                    IsCompleted = reader.GetBoolean(6),
                                    Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    Doctor = new Doctor
                                    {
                                        Id = reader.GetInt32(1),
                                        Name = reader.GetString(8),
                                        Specialty = reader.GetString(9),
                                        ImageUrl = reader.GetString(10),
                                        Description = reader.GetString(11)
                                    }
                                };
                                
                                allAppointments.Add(appointment);
                                Console.WriteLine($"Randevu bulundu: Id={appointment.Id}, Doktor={appointment.Doctor.Name}, Tarih={appointment.AppointmentDateTime}, Tamamlandı={appointment.IsCompleted}, Notlar={appointment.Notes}");
                            }
                        }
                    }
                }
                
                // Şimdiki tarih ve saat
                var currentDate = DateTime.Now;
                Console.WriteLine($"Şimdiki zaman: {currentDate}");
                
                // Tüm randevuları listele
                Console.WriteLine("Tüm randevular:");
                foreach (var apt in allAppointments)
                {
                    Console.WriteLine($"  - Id: {apt.Id}, Doktor: {apt.Doctor?.Name}, Tarih: {apt.AppointmentDateTime}, İptal: {apt.IsCancelled}, Tamamlandı: {apt.IsCompleted}");
                }
                
                // Debug mesajı ekle
                Console.WriteLine($"Bugünün tarihi: {currentDate.ToString("dd.MM.yyyy HH:mm")}");
                
                // Test: Her randevuyu manuel oluşturup yaklaşan olarak ekle
                var upcomingAppointments = new List<Appointment>();
                var pastAppointments = new List<Appointment>();
                
                // Her randevuyu doğru kategoriye ekle
                foreach (var apt in allAppointments)
                {
                    // Tamamlanmış randevuları her zaman geçmiş listeye ekle
                    if (apt.IsCompleted || apt.IsCancelled)
                    {
                        pastAppointments.Add(apt);
                        Console.WriteLine($"Tamamlanmış/iptal edilmiş olduğu için geçmiş randevulara eklendi: ID={apt.Id}, Doktor={apt.Doctor?.Name}, Tamamlandı={apt.IsCompleted}, İptal={apt.IsCancelled}");
                    }
                    // Gelecek randevuları yaklaşan listeye ekle
                    else if (apt.AppointmentDateTime > currentDate)
                    {
                        upcomingAppointments.Add(apt);
                        Console.WriteLine($"Yaklaşan randevulara eklendi: ID={apt.Id}, Doktor={apt.Doctor?.Name}, İptal={apt.IsCancelled}");
                    }
                    // Diğer tüm randevuları geçmiş listeye ekle
                    else
                    {
                        pastAppointments.Add(apt);
                        Console.WriteLine($"Tarihi geçtiği için geçmiş randevulara eklendi: ID={apt.Id}, Doktor={apt.Doctor?.Name}, İptal={apt.IsCancelled}");
                    }
                    
                    Console.WriteLine($"Randevu kontrolü: ID={apt.Id}, İptal={apt.IsCancelled}, Tamamlandı={apt.IsCompleted}, Tarih={apt.AppointmentDateTime}, Şimdi={currentDate}");
                }
                
                Console.WriteLine($"RANDEVU SAYILARI: Tüm: {allAppointments.Count}, Yaklaşan: {upcomingAppointments.Count}, Geçmiş: {pastAppointments.Count}");
                
                // Yaklaşan randevuları günlükle
                Console.WriteLine("Yaklaşan randevular:");
                foreach (var apt in upcomingAppointments)
                {
                    Console.WriteLine($"  - Id: {apt.Id}, Doktor: {apt.Doctor?.Name}, Tarih: {apt.AppointmentDateTime}");
                }
                
                Console.WriteLine($"Yaklaşan randevu sayısı: {upcomingAppointments.Count}");
                Console.WriteLine($"Geçmiş randevu sayısı: {pastAppointments.Count}");
                
                // Profil fotoğrafı kontrolü
                var patient = await _context.Patients.FindAsync(patientId);
                if (patient != null && !string.IsNullOrEmpty(patient.ProfilePhoto))
                {
                    ViewBag.ProfileImage = patient.ProfilePhoto;
                }
                
                // Debug bilgilerini ViewBag'e ekle
                ViewBag.DebugInfo = new Dictionary<string, object>
                {
                    { "TotalCount", allAppointments.Count },
                    { "UpcomingCount", upcomingAppointments.Count },
                    { "PastCount", pastAppointments.Count },
                    { "CurrentDate", currentDate.ToString("dd.MM.yyyy HH:mm:ss") },
                    { "RandevuIDs", string.Join(", ", allAppointments.Select(a => a.Id)) }
                };
                
                // ViewModel oluştur ve her randevuyu direkt ekle
                var viewModel = new MyAppointmentsViewModel();
                
                // Manuel olarak ekle
                foreach (var apt in upcomingAppointments)
                {
                    viewModel.UpcomingAppointments.Add(apt);
                    Console.WriteLine($"ViewModel'e manuel olarak randevu eklendi: ID={apt.Id}");
                }
                
                foreach (var apt in pastAppointments)
                {
                    viewModel.PastAppointments.Add(apt);
                }
                
                // Konsola detaylı bilgi
                Console.WriteLine("VIEW MODEL OLUŞTURULDU:");
                Console.WriteLine($"Yaklaşan randevu sayısı: {viewModel.UpcomingAppointments.Count}");
                foreach (var apt in viewModel.UpcomingAppointments)
                {
                    Console.WriteLine($"  ID: {apt.Id}, Tarih: {apt.AppointmentDateTime}, İptal: {apt.IsCancelled}");
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MyAppointments hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Randevularınız yüklenirken bir hata oluştu: " + ex.Message;
                return View(new MyAppointmentsViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin", "Account");
            }
            
            try
            {
                Console.WriteLine($"CancelAppointment çağrıldı: appointmentId={appointmentId}, patientId={patientId}");
                
                // Önce cache'i temizle
                _context.ChangeTracker.Clear();
                
                // Randevuyu bul
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == patientId);
                
                if (appointment == null)
                {
                    Console.WriteLine($"Randevu bulunamadı: ID={appointmentId}, PatientId={patientId}");
                    TempData["ErrorMessage"] = "Randevu bulunamadı veya bu randevu size ait değil.";
                    return RedirectToAction("MyAppointments");
                }
                
                Console.WriteLine($"Randevu bulundu: ID={appointment.Id}, İptal Durumu={appointment.IsCancelled}");
                
                // Geçmiş randevuları iptal etmeye izin verme
                if (appointment.AppointmentDateTime <= DateTime.Now)
                {
                    Console.WriteLine($"Geçmiş randevu iptal edilemez: ID={appointmentId}");
                    TempData["ErrorMessage"] = "Geçmiş randevular iptal edilemez.";
                    return RedirectToAction("MyAppointments");
                }
                
                // Randevuyu iptal et - SQLite doğrudan sorgu ile
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // DELETE sorgusu ile randevuyu tamamen sil
                    var commandText = "DELETE FROM Appointments WHERE Id = @appointmentId";
                    
                    using (var command = new Microsoft.Data.Sqlite.SqliteCommand(commandText, connection))
                    {
                        command.Parameters.AddWithValue("@appointmentId", appointmentId);
                        
                        // Sorguyu çalıştır ve etkilenen satır sayısını al
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        Console.WriteLine($"Silme sorgusu çalıştırıldı. Etkilenen satır: {rowsAffected}");
                        
                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Randevunuz başarıyla silindi.";
                            
                            // Veritabanından tekrar kontrol et
                            command.CommandText = "SELECT COUNT(*) FROM Appointments WHERE Id = @appointmentId";
                            var result = await command.ExecuteScalarAsync();
                            
                            Console.WriteLine($"Randevu kontrol sonucu (0 olmalı): {result}");
                        }
                        else
                        {
                            Console.WriteLine("Silme işlemi başarısız oldu. Hiçbir satır etkilenmedi.");
                            TempData["ErrorMessage"] = "Randevu silinemedi.";
                        }
                    }
                }
                
                return RedirectToAction("MyAppointments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Randevu iptal hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = "Randevu iptal edilirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("MyAppointments");
            }
        }

        [HttpGet]
        public IActionResult DebugLogin()
        {
            try
            {
                // Veritabanındaki hasta kaydını bul
                var patient = _context.Patients.FirstOrDefault();
                
                if (patient != null)
                {
                    // Session değişkenlerini ayarla
                    HttpContext.Session.SetInt32("PatientId", patient.Id);
                    HttpContext.Session.SetString("PatientName", patient.FullName);
                    HttpContext.Session.SetString("UserRole", "Patient");
                    HttpContext.Session.SetString("UserEmail", patient.Email);
                    
                    TempData["SuccessMessage"] = $"Otomatik giriş yapıldı. Hoş geldiniz, {patient.FullName}";
                    Console.WriteLine($"Debug Login: PatientId={patient.Id}, Name={patient.FullName}");
                    
                    return RedirectToAction("MyAppointments");
                }
                else
                {
                    TempData["ErrorMessage"] = "Veritabanında hasta kaydı bulunamadı!";
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Giriş yapılırken hata: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult DebugRandevular()
        {
            // Session bilgileri
            var patientId = HttpContext.Session.GetInt32("PatientId");
            var patientName = HttpContext.Session.GetString("PatientName");
            
            // View'e model olarak sonuçları göndermek için kullanılacak
            var debugModel = new Dictionary<string, object>();
            
            debugModel.Add("SessionPatientId", patientId);
            debugModel.Add("SessionPatientName", patientName);
            
            try
            {
                // Tüm randevuları listele
                var allAppointments = _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .ToList();
                
                debugModel.Add("TotalAppointments", allAppointments.Count);
                debugModel.Add("Appointments", allAppointments);
                
                // Hastayı bul
                var patient = _context.Patients.FirstOrDefault(p => p.Id == patientId);
                debugModel.Add("Patient", patient);
                
                // Doktorları listele
                var doctors = _context.Doctors.ToList();
                debugModel.Add("TotalDoctors", doctors.Count);
                
                // Tüm tabloları göster
                var tableNames = _context.Model.GetEntityTypes()
                    .Select(t => t.GetTableName())
                    .ToList();
                debugModel.Add("DatabaseTables", tableNames);
                
                return View(debugModel);
            }
            catch (Exception ex)
            {
                debugModel.Add("Error", ex.Message);
                debugModel.Add("StackTrace", ex.StackTrace);
                return View(debugModel);
            }
        }
    }
}
