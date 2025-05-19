using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hastane.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Bölüm adı zorunludur")]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(200)]
        public string ImageUrl { get; set; }
    }
} 