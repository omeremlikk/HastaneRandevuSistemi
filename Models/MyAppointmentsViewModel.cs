using System;
using System.Collections.Generic;

namespace hastane.Models
{
    public class MyAppointmentsViewModel
    {
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> PastAppointments { get; set; } = new List<Appointment>();
    }
} 