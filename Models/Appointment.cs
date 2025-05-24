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
        public Appointment()
        {
            PatientName = string.Empty;
            PatientPhone = string.Empty;
            PatientEmail = string.Empty;
            Notes = string.Empty;
            CreatedAt = DateTime.Now;
            Status = AppointmentStatus.Pending;
        }

        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Doktor seçimi zorunludur")]
        public int DoctorId { get; set; }
        
        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }
        
        [Required(ErrorMessage = "Randevu tarihi zorunludur")]
        [DataType(DataType.Date)]
        [Display(Name = "Randevu Tarihi")]
        public DateTime Date { get; set; }
        
        [Required(ErrorMessage = "Randevu saati zorunludur")]
        [DataType(DataType.Time)]
        [Display(Name = "Randevu Saati")]
        public TimeSpan Time { get; set; }
        
        [Required(ErrorMessage = "Hasta adı zorunludur")]
        [StringLength(100)]
        [Display(Name = "Hasta Adı")]
        public string PatientName { get; set; }
        
        [Required(ErrorMessage = "Telefon numarası zorunludur")]
        [StringLength(20)]
        [Phone]
        [Display(Name = "Telefon")]
        public string PatientPhone { get; set; }
        
        [Required(ErrorMessage = "E-posta adresi zorunludur")]
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string PatientEmail { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Notlar")]
        public string Notes { get; set; }
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Display(Name = "Durum")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    }
}
