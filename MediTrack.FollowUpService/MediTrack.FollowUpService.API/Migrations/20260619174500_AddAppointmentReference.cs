using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediTrack.FollowUpService.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointment_reference",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_reference", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_reference");
        }
    }
}
