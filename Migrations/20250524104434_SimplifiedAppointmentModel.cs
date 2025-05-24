using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hastane.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedAppointmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientEmail",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientPhone",
                table: "Appointments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientEmail",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "Appointments",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PatientPhone",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
