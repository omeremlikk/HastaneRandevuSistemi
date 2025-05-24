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
        
        [Required]
        public int DoctorId { get; set; }
        
        [Required]
        public DateTime AppointmentDateTime { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
    }
}
