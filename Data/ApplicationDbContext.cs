using Microsoft.EntityFrameworkCore;
using hastane.Models;

namespace hastane.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Doktor-Randevu ilişkisi
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Departments
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Dahiliye", Description = "İç hastalıkları tanı ve tedavisi", ImageUrl = "/img/departments/internal.jpg" },
                new Department { Id = 2, Name = "KBB", Description = "Kulak, burun ve boğaz hastalıkları", ImageUrl = "/img/departments/ent.jpg" },
                new Department { Id = 3, Name = "Kardiyoloji", Description = "Kalp ve damar hastalıkları", ImageUrl = "/img/departments/cardiology.jpg" },
                new Department { Id = 4, Name = "Dermatoloji", Description = "Cilt hastalıkları tanı ve tedavisi", ImageUrl = "/img/departments/dermatology.jpg" },
                new Department { Id = 5, Name = "Göz Hastalıkları", Description = "Göz ve görme ile ilgili hastalıklar", ImageUrl = "/img/departments/ophthalmology.jpg" },
                new Department { Id = 6, Name = "Ortopedi", Description = "Kas ve iskelet sistemi hastalıkları", ImageUrl = "/img/departments/orthopedics.jpg" },
                new Department { Id = 7, Name = "Nöroloji", Description = "Sinir sistemi hastalıkları", ImageUrl = "/img/departments/neurology.jpg" },
                new Department { Id = 8, Name = "Psikiyatri", Description = "Ruh sağlığı ve hastalıkları", ImageUrl = "/img/departments/psychiatry.jpg" },
                new Department { Id = 9, Name = "Üroloji", Description = "Üriner sistem hastalıkları", ImageUrl = "/img/departments/urology.jpg" },
                new Department { Id = 10, Name = "Kadın Hastalıkları ve Doğum", Description = "Kadın üreme sistemi ve gebelik takibi", ImageUrl = "/img/departments/gynecology.jpg" },
                new Department { Id = 11, Name = "Endokrinoloji", Description = "Hormon ve metabolizma hastalıkları", ImageUrl = "/img/departments/endocrinology.jpg" }
            );

            // Seed Doctors
            modelBuilder.Entity<Doctor>().HasData(
                new Doctor { Id = 1, Name = "Dr. Ahmet Yılmaz", Specialty = "Dahiliye", ImageUrl = "/img/doctors/doctor1.jpg" },
                new Doctor { Id = 2, Name = "Dr. Mehmet Kaya", Specialty = "KBB", ImageUrl = "/img/doctors/doctor3.jpg" },
                new Doctor { Id = 3, Name = "Dr. Ali Öztürk", Specialty = "Kardiyoloji", ImageUrl = "/img/doctors/doctor2.jpg" },
                new Doctor { Id = 4, Name = "Dr. Can Yücel", Specialty = "Dermatoloji", ImageUrl = "/img/doctors/doctor7.jpg" },
                new Doctor { Id = 5, Name = "Dr. Ece Şahin", Specialty = "Göz Hastalıkları", ImageUrl = "/img/doctors/doctor9.jpg" },
                new Doctor { Id = 6, Name = "Dr. Gamze Özkan", Specialty = "Ortopedi", ImageUrl = "/img/doctors/doctor11.jpg" },
                new Doctor { Id = 7, Name = "Dr. İrem Doğan", Specialty = "Nöroloji", ImageUrl = "/img/doctors/doctor13.jpg" },
                new Doctor { Id = 8, Name = "Dr. Kemal Tunç", Specialty = "Psikiyatri", ImageUrl = "/img/doctors/doctor15.jpg" },
                new Doctor { Id = 9, Name = "Dr. Murat Ersoy", Specialty = "Üroloji", ImageUrl = "/img/doctors/doctor17.jpg" },
                new Doctor { Id = 10, Name = "Dr. Osman Kara", Specialty = "Kadın Hastalıkları ve Doğum", ImageUrl = "/img/doctors/doctor19.jpg" },
                new Doctor { Id = 11, Name = "Dr. Rıza Altın", Specialty = "Endokrinoloji", ImageUrl = "/img/doctors/doctor21.jpg" }
            );
        }
    }
} 