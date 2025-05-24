using System;
using System.Collections.Generic;

namespace hastane.Models
{
    public class AppointmentViewModel
    {
        public AppointmentViewModel()
        {
            Doctor = new Doctor();
            Appointment = new Appointment();
        }

        public Doctor Doctor { get; set; }
        public Appointment Appointment { get; set; }
    }
} 