using System;
using System.ComponentModel.DataAnnotations;

namespace hastane.Models
{
    public class AppointmentViewModel
    {
        public AppointmentViewModel()
        {
            Doctor = new Doctor();
            Appointment = new Appointment();
        }

        public Doctor? Doctor { get; set; }
        
        [Required(ErrorMessage = "Randevu bilgileri gereklidir.")]
        public Appointment Appointment { get; set; }
    }
} 