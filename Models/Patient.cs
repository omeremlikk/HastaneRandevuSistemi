using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hastane.Models
{
    public class Patient
    {
        public Patient()
        {
            FullName = string.Empty;
            IdentityNumber = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Password = string.Empty;
            Appointments = new List<Appointment>();
        }

        public int Id { get; set; }
        
        [Required]
        public string FullName { get; set; }
        
        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string IdentityNumber { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [Phone]
        public string Phone { get; set; }
        
        [Required]
        public string Password { get; set; }
        
        public DateTime RegisterDate { get; set; } = DateTime.Now;
        
        // İlişkiler
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
} 