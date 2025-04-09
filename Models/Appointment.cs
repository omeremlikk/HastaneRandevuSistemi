using System;

namespace hastane.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public string PatientEmail { get; set; }
        public string Notes { get; set; }
    }
} 