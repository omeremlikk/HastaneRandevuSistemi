using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace hastane.Models
{
    public class Doctor
    {
        public Doctor()
        {
            // Zorunlu alanları başlatmak için varsayılan değerler
            Name = string.Empty;
            Description = string.Empty;
            Specialty = string.Empty;
            ImageUrl = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            Appointments = new List<Appointment>();
            Availabilities = new List<DoctorAvailability>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "İsim zorunludur")]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Uzmanlık alanı zorunludur")]
        [StringLength(100)]
        public string Specialty { get; set; }
        
        public string ImageUrl { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        // Giriş bilgileri
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        // Navigation properties
        public ICollection<Appointment>? Appointments { get; set; }
        
        // Müsaitlik durumları
        public ICollection<DoctorAvailability> Availabilities { get; set; }

        // Doktorun belirli bir tarih ve saatte müsait olup olmadığını kontrol eder
        public bool IsAvailable(DateTime date, string time)
        {
            try
            {
                // Hafta sonu kontrolü - Cumartesi veya Pazar ise asla müsait değil
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    Console.WriteLine($"IsAvailable: {date.ToString("yyyy-MM-dd")} tarihi hafta sonu, randevu alınamaz.");
                    return false;
                }

                // Geçmiş tarih kontrolü
                if (date.Date < DateTime.Today)
                    return false;

                // Çalışma saatleri kontrolü (09:00-17:00)
                var requestedTime = TimeSpan.Parse(time);
                if (requestedTime < TimeSpan.FromHours(9) || requestedTime > TimeSpan.FromHours(17))
                    return false;

                // Randevu kontrolü - aynı gün ve saatte başka randevu var mı?
                if (Appointments == null || !Appointments.Any())
                    return true;

                // Kesin tarih ve saat kontrolü - aynı gün ve saatte randevu var mı?
                foreach (var appointment in Appointments)
                {
                    if (appointment.AppointmentDateTime.Date == date.Date && 
                        appointment.AppointmentDateTime.ToString("HH:mm") == time)
                    {
                        // Bu saatte randevu var, müsait değil
                        return false;
                    }
                }
                
                return true; // Bu saatte randevu yok, müsait
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IsAvailable metodu hatası: {ex.Message}");
                return false; // Hata durumunda güvenli tarafta kalmak için false döndür
            }
        }

        // Doktorun belirli bir tarihteki müsait saatlerini döndürür
        public List<string> GetAvailableTimeSlots(DateTime date)
        {
            var availableSlots = new List<string>();

            // Hafta sonu kontrolü - Cumartesi veya Pazar ise boş liste döndür
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                Console.WriteLine($"GetAvailableTimeSlots: {date.ToString("yyyy-MM-dd")} tarihi hafta sonu, müsait saat yok.");
                return availableSlots; // Boş liste
            }

            // Geçmiş tarih kontrolü
            if (date.Date < DateTime.Today)
            {
                return availableSlots; // Boş liste
            }

            // Tüm saat aralıkları
            var allTimeSlots = new List<string> {
                "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30"
            };

            // Her saat için müsaitlik kontrolü
            foreach (var time in allTimeSlots)
            {
                if (IsAvailable(date, time))
                {
                    availableSlots.Add(time);
                }
            }

            return availableSlots;
        }
    }
} 