using System;
using System.Collections.Generic;

namespace hastane.Models
{
    public class AppointmentViewModel
    {
        public Appointment Appointment { get; set; }
        public List<Doctor> Doctors { get; set; }
        public List<Department> Departments { get; set; }
    }
} 