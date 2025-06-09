using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hastane.Models
{
    public enum AppointmentStatus
    {
        [Display(Name = "Beklemede")]
        Pending,
        
        [Display(Name = "Onaylandı")]
        Confirmed,
        
        [Display(Name = "İptal Edildi")]
        Cancelled,
        
        [Display(Name = "Tamamlandı")]
        Completed
    }

    public class Appointment
    {
        public int Id { get; set; }
        
        public int DoctorId { get; set; }
        
        public int PatientId { get; set; }
        
        public DateTime AppointmentDateTime { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public bool IsCancelled { get; set; } = false;
        
        public bool IsCompleted { get; set; } = false;
        
        public string? Notes { get; set; }
        
        // İlişkiler
        public Doctor? Doctor { get; set; }
        
        public Patient? Patient { get; set; }
    }
}
