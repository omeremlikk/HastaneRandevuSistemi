using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using hastane.Models;
using hastane.Data;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace hastane.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Email veya TC Kimlik numarası zaten var mı kontrol et
                if (_context.Patients.Any(p => p.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                    return View(model);
                }

                if (_context.Patients.Any(p => p.IdentityNumber == model.IdentityNumber))
                {
                    ModelState.AddModelError("IdentityNumber", "Bu TC Kimlik numarası zaten kullanılıyor.");
                    return View(model);
                }

                // Yeni hasta oluştur - şifre düz metin olarak saklanıyor
                var patient = new Patient
                {
                    FullName = model.FullName,
                    IdentityNumber = model.IdentityNumber,
                    Email = model.Email,
                    Phone = model.Phone,
                    Password = model.Password, // Şifre direkt saklanıyor
                    RegisterDate = DateTime.Now
                };
                
                _context.Patients.Add(patient);
                _context.SaveChanges();
                
                // Oturum bilgisini sakla
                HttpContext.Session.SetInt32("PatientId", patient.Id);
                HttpContext.Session.SetString("PatientName", patient.FullName);
                HttpContext.Session.SetString("UserRole", "Patient");
                HttpContext.Session.SetString("UserEmail", patient.Email);
                
                return RedirectToAction("RegistrationSuccess");
            }
            
            return View(model);
        }
        
        public IActionResult RegistrationSuccess()
        {
            return View();
        }
        
        public IActionResult DoctorLogin()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult DoctorLogin(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctor = _context.Doctors.FirstOrDefault(d => d.Email == model.Email);
                
                if (doctor != null && doctor.Password == model.Password) // Düz metin karşılaştırma
                {
                    // Oturum bilgisini sakla
                    HttpContext.Session.SetInt32("DoctorId", doctor.Id);
                    HttpContext.Session.SetString("DoctorName", doctor.Name);
                    HttpContext.Session.SetString("UserRole", "Doctor");
                    
                    return RedirectToAction("Dashboard", "Doctor");
                }
                
                // Hata durumunda uyarı göster
                if (doctor == null)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi ile kayıtlı doktor bulunamadı.");
                }
                else
                {
                    ModelState.AddModelError("Password", "Şifre yanlış. Lütfen tekrar deneyin.");
                }
                
                TempData["ErrorMessage"] = "Giriş yapılamadı. Lütfen e-posta ve şifrenizi kontrol edin.";
            }
            
            return View(model);
        }
        
        public IActionResult PatientLogin()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult PatientLogin(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patient = _context.Patients.FirstOrDefault(p => p.Email == model.Email);
                
                if (patient != null && patient.Password == model.Password) // Düz metin karşılaştırma
                {
                    // Oturum bilgisini sakla
                    HttpContext.Session.SetInt32("PatientId", patient.Id);
                    HttpContext.Session.SetString("PatientName", patient.FullName);
                    HttpContext.Session.SetString("UserRole", "Patient");
                    HttpContext.Session.SetString("UserEmail", patient.Email);
                    
                return RedirectToAction("Index", "Home");
                }
                
                // Hata durumunda uyarı göster
                if (patient == null)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı.");
                }
                else
                {
                    ModelState.AddModelError("Password", "Şifre yanlış. Lütfen tekrar deneyin.");
                }
                
                TempData["ErrorMessage"] = "Giriş yapılamadı. Lütfen e-posta ve şifrenizi kontrol edin.";
            }
            
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Profile()
        {
            // Kullanıcı girişi kontrolü
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin");
            }

            // Kullanıcı bilgilerini getir
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null)
            {
                return NotFound();
            }

            // Profil fotoğrafı varsa ViewBag'e ekle
            if (!string.IsNullOrEmpty(patient.ProfilePhoto))
            {
                ViewBag.ProfileImage = patient.ProfilePhoto;
            }

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Patient model)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin");
            }

            try
            {
                // Mevcut hasta bilgilerini getir
                var existingPatient = await _context.Patients
                    .AsNoTracking() // Önce tracking olmadan getir
                    .FirstOrDefaultAsync(p => p.Id == patientId.Value);
                    
                if (existingPatient == null)
                {
                    return NotFound();
                }
                
                Console.WriteLine($"Mevcut bilgiler: Ad={existingPatient.FullName}, Tel={existingPatient.Phone}");
                Console.WriteLine($"Yeni bilgiler: Ad={model.FullName}, Tel={model.Phone}");

                // ModelState'den TC Kimlik numarası ve e-posta hatasını kaldır (değiştiremez)
                ModelState.Remove("IdentityNumber");
                ModelState.Remove("Email");
                ModelState.Remove("Password"); // Şifre alanını da doğrulama dışı bırak
                
                if (ModelState.IsValid)
                {
                    // Güncellenecek hasta nesnesini yeniden getir
                    var patientToUpdate = await _context.Patients.FindAsync(patientId);
                    
                    // Güncellenecek alanları ayarla
                    patientToUpdate.FullName = model.FullName;
                    patientToUpdate.Phone = model.Phone;
                    // E-posta, TC Kimlik ve şifre güncellenmez

                    // Durumu işaretle ve kaydet
                    _context.Entry(patientToUpdate).State = EntityState.Modified;
                    
                    var result = await _context.SaveChangesAsync();
                    Console.WriteLine($"SaveChanges sonucu: {result} kayıt değişti.");

                    // Session'daki kullanıcı adını güncelle
                    HttpContext.Session.SetString("PatientName", patientToUpdate.FullName);

                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                    return RedirectToAction("Profile");
                }
                else
                {
                    var errors = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    Console.WriteLine($"Model geçerli değil: {errors}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Profil güncellenirken hata: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Profil güncellenirken bir hata oluştu: " + ex.Message;
            }

            // Hata durumunda mevcut bilgileri tekrar göster
            var currentPatient = await _context.Patients.FindAsync(patientId);
            return View("Profile", currentPatient); // model yerine currentPatient gönderiyoruz
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile profilePhoto)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin");
            }

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound();
            }

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                // Dosya boyutu kontrolü (max 2MB)
                if (profilePhoto.Length > 2 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Dosya boyutu 2MB'dan küçük olmalıdır.";
                    return RedirectToAction("Profile");
                }

                // Dosya türü kontrolü
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Sadece JPG, PNG ve GIF dosyaları desteklenir.";
                    return RedirectToAction("Profile");
                }

                try
                {
                    // Eski fotoğrafı sil (eğer varsa ve uploads klasöründeyse)
                    if (!string.IsNullOrEmpty(patient.ProfilePhoto) && 
                        !patient.ProfilePhoto.StartsWith("http"))
                    {
                        var oldPhotoPath = Path.Combine(_webHostEnvironment.WebRootPath, patient.ProfilePhoto.TrimStart('/'));
                        if (System.IO.File.Exists(oldPhotoPath))
                        {
                            System.IO.File.Delete(oldPhotoPath);
                        }
                    }

                    // Yeni dosya adı oluştur
                    var fileName = $"patient_{patientId}_{Guid.NewGuid()}{extension}";
                    
                    // Uploads klasörü yoksa oluştur
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Dosyayı kaydet
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePhoto.CopyToAsync(stream);
                    }

                    // Veritabanındaki yolu güncelle
                    patient.ProfilePhoto = $"/uploads/profiles/{fileName}";
                    _context.Update(patient);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Profil fotoğrafınız başarıyla güncellendi.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Fotoğraf yüklenirken bir hata oluştu: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Lütfen bir fotoğraf seçin.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<JsonResult> RemoveProfilePhoto()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return Json(new { success = false, message = "Giriş yapılmamış." });
            }

            try
            {
                var patient = await _context.Patients.FindAsync(patientId);
                if (patient == null)
                {
                    return Json(new { success = false, message = "Hasta bulunamadı." });
                }

                // Fotoğrafın dosya sisteminden silinmesi
                if (!string.IsNullOrEmpty(patient.ProfilePhoto) && 
                    !patient.ProfilePhoto.StartsWith("http"))
                {
                    var photoPath = Path.Combine(_webHostEnvironment.WebRootPath, patient.ProfilePhoto.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                    {
                        System.IO.File.Delete(photoPath);
                    }
                }

                // Veritabanından referansı kaldır
                patient.ProfilePhoto = null;
                _context.Update(patient);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> ChangePassword()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin");
            }

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound();
            }

            // Profil fotoğrafı varsa ViewBag'e ekle
            if (!string.IsNullOrEmpty(patient.ProfilePhoto))
            {
                ViewBag.ProfileImage = patient.ProfilePhoto;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                return RedirectToAction("PatientLogin");
            }

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return NotFound();
            }
            
            // Profil fotoğrafı varsa ViewBag'e ekle (hata durumunda sayfayı yeniden gösterirken)
            if (!string.IsNullOrEmpty(patient.ProfilePhoto))
            {
                ViewBag.ProfileImage = patient.ProfilePhoto;
            }

            // Doğrulama kontrolleri
            if (string.IsNullOrEmpty(currentPassword))
            {
                TempData["ErrorMessage"] = "Mevcut şifrenizi girmelisiniz.";
                return View();
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Yeni şifreniz en az 6 karakter uzunluğunda olmalıdır.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Yeni şifre ve onay şifresi eşleşmiyor.";
                return View();
            }

            if (patient.Password != currentPassword)
            {
                TempData["ErrorMessage"] = "Mevcut şifreniz hatalı.";
                return View();
            }

            try
            {
                // Şifreyi güncelle
                patient.Password = newPassword;
                _context.Update(patient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Şifre değiştirme işlemi sırasında bir hata oluştu: " + ex.Message;
                return View();
            }
        }
    }
} 