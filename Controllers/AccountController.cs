using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using hastane.Models;

namespace hastane.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Burada kullanıcı kaydı yapılır (gerçek uygulamada veritabanına kaydedilir)
                
                // Başarılı kayıt sonrası yönlendirme
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
                // Burada doktor girişi doğrulanır
                
                // Başarılı giriş sonrası yönlendirme
                return RedirectToAction("Index", "Home");
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
                // Burada hasta girişi doğrulanır
                
                // Başarılı giriş sonrası yönlendirme
                return RedirectToAction("Index", "Home");
            }
            
            return View(model);
        }
    }
} 