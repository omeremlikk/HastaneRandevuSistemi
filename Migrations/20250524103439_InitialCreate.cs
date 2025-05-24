using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace hastane.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DepartmentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AppointmentDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PatientPhone = table.Column<string>(type: "TEXT", nullable: false),
                    PatientEmail = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorAvailability",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DoctorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppointmentDuration = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorAvailability_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Description", "ImageUrl", "Name" },
                values: new object[,]
                {
                    { 1, "İç hastalıkları tanı ve tedavisi", "/img/departments/internal.jpg", "Dahiliye" },
                    { 2, "Kulak, burun ve boğaz hastalıkları", "/img/departments/ent.jpg", "KBB" },
                    { 3, "Kalp ve damar hastalıkları", "/img/departments/cardiology.jpg", "Kardiyoloji" },
                    { 4, "Cilt hastalıkları tanı ve tedavisi", "/img/departments/dermatology.jpg", "Dermatoloji" },
                    { 5, "Göz ve görme ile ilgili hastalıklar", "/img/departments/ophthalmology.jpg", "Göz Hastalıkları" },
                    { 6, "Kas ve iskelet sistemi hastalıkları", "/img/departments/orthopedics.jpg", "Ortopedi" },
                    { 7, "Sinir sistemi hastalıkları", "/img/departments/neurology.jpg", "Nöroloji" },
                    { 8, "Ruh sağlığı ve hastalıkları", "/img/departments/psychiatry.jpg", "Psikiyatri" },
                    { 9, "Üriner sistem hastalıkları", "/img/departments/urology.jpg", "Üroloji" },
                    { 10, "Kadın üreme sistemi ve gebelik takibi", "/img/departments/gynecology.jpg", "Kadın Hastalıkları ve Doğum" },
                    { 11, "Hormon ve metabolizma hastalıkları", "/img/departments/endocrinology.jpg", "Endokrinoloji" }
                });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "DepartmentId", "Description", "ImageUrl", "Name", "Specialty" },
                values: new object[,]
                {
                    { 1, null, "", "/img/doctors/doctor1.jpg", "Dr. Ahmet Yılmaz", "Dahiliye" },
                    { 2, null, "", "/img/doctors/doctor3.jpg", "Dr. Mehmet Kaya", "KBB" },
                    { 3, null, "", "/img/doctors/doctor2.jpg", "Dr. Ali Öztürk", "Kardiyoloji" },
                    { 4, null, "", "/img/doctors/doctor7.jpg", "Dr. Can Yücel", "Dermatoloji" },
                    { 5, null, "", "/img/doctors/doctor9.jpg", "Dr. Ece Şahin", "Göz Hastalıkları" },
                    { 6, null, "", "/img/doctors/doctor11.jpg", "Dr. Gamze Özkan", "Ortopedi" },
                    { 7, null, "", "/img/doctors/doctor13.jpg", "Dr. İrem Doğan", "Nöroloji" },
                    { 8, null, "", "/img/doctors/doctor15.jpg", "Dr. Kemal Tunç", "Psikiyatri" },
                    { 9, null, "", "/img/doctors/doctor17.jpg", "Dr. Murat Ersoy", "Üroloji" },
                    { 10, null, "", "/img/doctors/doctor19.jpg", "Dr. Osman Kara", "Kadın Hastalıkları ve Doğum" },
                    { 11, null, "", "/img/doctors/doctor21.jpg", "Dr. Rıza Altın", "Endokrinoloji" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailability_DoctorId",
                table: "DoctorAvailability",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_DepartmentId",
                table: "Doctors",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DoctorAvailability");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
