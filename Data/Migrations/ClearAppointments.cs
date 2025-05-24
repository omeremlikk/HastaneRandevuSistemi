using Microsoft.EntityFrameworkCore.Migrations;

namespace hastane.Data.Migrations
{
    public partial class ClearAppointments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tüm randevuları temizle
            migrationBuilder.Sql("DELETE FROM Appointments");
            
            // DoctorAvailabilities tablosunu temizle
            migrationBuilder.Sql("DELETE FROM DoctorAvailabilities");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alma işlemi yok
        }
    }
} 