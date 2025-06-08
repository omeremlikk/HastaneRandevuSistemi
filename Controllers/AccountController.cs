using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using hastane.Models;
using hastane.Data;

namespace hastane.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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
                    
                    return RedirectToAction("Index", "Home");
                }
                
                ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
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
                    
                    return RedirectToAction("Index", "Home");
                }
                
                ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
            }
            
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
} 