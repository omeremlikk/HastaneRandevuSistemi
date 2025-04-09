using System;
using System.Collections.Generic;

namespace hastane.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
    }
} 