using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hastane.Models
{
    public class DoctorAvailability
    {
        public DoctorAvailability()
        {
            Doctor = new Doctor();
            // Varsayılan değerler
            StartTime = TimeSpan.FromHours(9); // 09:00
            EndTime = TimeSpan.FromHours(17);  // 17:00
            DayOfWeek = DayOfWeek.Monday;
            IsAvailable = true;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public bool IsAvailable { get; set; }

        // Randevu süresi (dakika cinsinden)
        [Required]
        public int AppointmentDuration { get; set; } = 30; // Varsayılan randevu süresi 30 dakika
    }
} 