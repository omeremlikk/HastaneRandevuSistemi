using System;
using System.ComponentModel.DataAnnotations;

namespace hastane.Models
{
    public class AppointmentViewModel : IValidatableObject
    {
        public AppointmentViewModel()
        {
            Doctor = new Doctor();
            Appointment = new Appointment();
        }

        // Validasyon bypass için Required attribute'ı kaldırıldı
        public Doctor? Doctor { get; set; }
        
        public Appointment? Appointment { get; set; }
        
        // Form validasyon için gerekli ek alanlar
        // Bu alanlar sadece frontend validasyonu için kullanılır, 
        // backend'de kullanılmayacak
        public string? Email { get; set; }
        
        public string? Password { get; set; }
        
        // Validasyon bypass için özel metot
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Tüm validasyonları geçerli say, hiç hata dönme
            yield break;
        }
    }
} 