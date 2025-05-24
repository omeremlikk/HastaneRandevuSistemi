using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace hastane.Models
{
    public class Department
    {
        public Department()
        {
            Name = string.Empty;
            Description = string.Empty;
            ImageUrl = string.Empty;
            Doctors = new List<Doctor>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        public string ImageUrl { get; set; }
        
        public virtual ICollection<Doctor> Doctors { get; set; }
    }
} 