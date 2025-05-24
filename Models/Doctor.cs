using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hastane.Models
{
    public class Doctor
    {
        public Doctor()
        {
            Name = string.Empty;
            Specialty = string.Empty;
            ImageUrl = string.Empty;
            Description = string.Empty;
            Appointments = new List<Appointment>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Doktor adı zorunludur")]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Uzmanlık alanı zorunludur")]
        [StringLength(100)]
        public string Specialty { get; set; }
        
        [StringLength(200)]
        public string ImageUrl { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        // Navigation properties
        public virtual ICollection<Appointment> Appointments { get; set; }
    }
} 