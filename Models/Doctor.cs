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
            Name = string.Empty;
            Specialty = string.Empty;
            ImageUrl = string.Empty;
            Description = string.Empty;
            Appointments = new List<Appointment>();
            Availabilities = new List<DoctorAvailability>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Specialty { get; set; } = string.Empty;
        
        public string ImageUrl { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; }
        
        // Navigation properties
        public virtual ICollection<Appointment>? Appointments { get; set; }
        
        // Müsaitlik durumları
        public virtual ICollection<DoctorAvailability> Availabilities { get; set; }

        // Doktorun belirli bir tarih ve saatte müsait olup olmadığını kontrol eder
        public bool IsAvailable(DateTime date, string time)
        {
            // Hafta sonu kontrolü
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // Geçmiş tarih kontrolü
            if (date.Date < DateTime.Today)
                return false;

            // Çalışma saatleri kontrolü (09:00-17:00)
            var requestedTime = TimeSpan.Parse(time);
            if (requestedTime < TimeSpan.FromHours(9) || requestedTime > TimeSpan.FromHours(17))
                return false;

            // O saatte randevu var mı kontrolü
            if (Appointments == null) return true;

            var requestedDateTime = date.Date.Add(requestedTime);
            return !Appointments.Any(a => a.AppointmentDateTime == requestedDateTime);
        }

        // Doktorun belirli bir tarihteki müsait saatlerini döndürür
        public List<string> GetAvailableTimeSlots(DateTime date)
        {
            var availableSlots = new List<string>();

            // Hafta sonu veya geçmiş tarih kontrolü
            if (date.DayOfWeek == DayOfWeek.Saturday || 
                date.DayOfWeek == DayOfWeek.Sunday || 
                date.Date < DateTime.Today)
            {
                return availableSlots;
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