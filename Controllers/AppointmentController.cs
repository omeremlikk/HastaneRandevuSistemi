using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using hastane.Models;

namespace hastane.Controllers
{
    public class AppointmentController : Controller
    {
        // Örnek veri - gerçek uygulamada veritabanından gelecek
        private static List<Doctor> doctors = new List<Doctor>
        {
            new Doctor { Id = 1, Name = "Dr. Ahmet Yılmaz", Specialty = "Kardiyoloji", ImageUrl = "/img/doctors/doctor1.jpg", Description = "Kalp ve damar hastalıkları uzmanı" },
            new Doctor { Id = 2, Name = "Dr. Ayşe Kaya", Specialty = "Nöroloji", ImageUrl = "/img/doctors/doctor2.jpg", Description = "Sinir sistemi hastalıkları uzmanı" },
            new Doctor { Id = 3, Name = "Dr. Mehmet Demir", Specialty = "Ortopedi", ImageUrl = "/img/doctors/doctor3.jpg", Description = "Kas ve iskelet sistemi hastalıkları uzmanı" },
            new Doctor { Id = 4, Name = "Dr. Zeynep Aksoy", Specialty = "Göz Hastalıkları", ImageUrl = "/img/doctors/doctor4.jpg", Description = "Göz hastalıkları uzmanı" },
        };

        private static List<Department> departments = new List<Department>
        {
            new Department { Id = 1, Name = "Kardiyoloji", Description = "Kalp ve damar hastalıkları bölümü", ImageUrl = "/img/departments/cardiology.jpg" },
            new Department { Id = 2, Name = "Nöroloji", Description = "Sinir sistemi hastalıkları bölümü", ImageUrl = "/img/departments/neurology.jpg" },
            new Department { Id = 3, Name = "Ortopedi", Description = "Kas ve iskelet sistemi hastalıkları bölümü", ImageUrl = "/img/departments/orthopedics.jpg" },
            new Department { Id = 4, Name = "Göz Hastalıkları", Description = "Göz hastalıkları bölümü", ImageUrl = "/img/departments/ophthalmology.jpg" },
            new Department { Id = 5, Name = "Dahiliye", Description = "İç hastalıkları bölümü", ImageUrl = "/img/departments/internal.jpg" },
        };

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Doctors()
        {
            return View(doctors);
        }

        public IActionResult Departments()
        {
            return View(departments);
        }

        public IActionResult Book()
        {
            var viewModel = new AppointmentViewModel
            {
                Doctors = doctors,
                Departments = departments,
                Appointment = new Appointment()
            };
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Book(Appointment appointment)
        {
            // Burada randevu kaydı yapılabilir, şimdilik sadece teşekkür sayfasına yönlendiriyoruz
            return RedirectToAction("ThankYou");
        }

        public IActionResult ThankYou()
        {
            return View();
        }

        public IActionResult DoctorDetails(int id)
        {
            var doctor = doctors.Find(d => d.Id == id);
            return View(doctor);
        }
    }
} 