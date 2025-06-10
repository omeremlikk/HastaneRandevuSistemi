using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hastane.Models
{
    // Doktor Dashboard için ViewModel
    public class DoctorDashboardViewModel
    {
        public DoctorDashboardViewModel()
        {
            TodayAppointments = new List<Appointment>();
            UpcomingAppointments = new List<Appointment>();
        }

        public List<Appointment> TodayAppointments { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; }
    }

    // Doktor Tüm Randevular için ViewModel
    public class DoctorAppointmentsViewModel
    {
        public DoctorAppointmentsViewModel()
        {
            TodayAppointments = new List<Appointment>();
            UpcomingAppointments = new List<Appointment>();
            PastAppointments = new List<Appointment>();
            CancelledAppointments = new List<Appointment>();
        }

        public List<Appointment> TodayAppointments { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; }
        public List<Appointment> PastAppointments { get; set; }
        public List<Appointment> CancelledAppointments { get; set; }
    }
} 