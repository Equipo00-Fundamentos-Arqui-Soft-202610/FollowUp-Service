using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MediTrack.FollowUpService.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointment_compliances",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    attended = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    notes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_compliances", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    dose = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    frequency_hour = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    stock_count = table.Column<int>(type: "int", nullable: false),
                    stock_alert_threshold = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medications", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "offline_sync_queue",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    entity_type = table.Column<string>(type: "longtext", nullable: false),
                    payload = table.Column<string>(type: "json", nullable: false),
                    queued_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    synced_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offline_sync_queue", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dose_schedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    medication_id = table.Column<int>(type: "int", nullable: false),
                    scheduled_time = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dose_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_dose_schedules_medications_medication_id",
                        column: x => x.medication_id,
                        principalTable: "medications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "medication_compliances",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    dose_schedule_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    recorded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    video_url = table.Column<string>(type: "longtext", nullable: true),
                    synced = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    offline_recorded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_compliances", x => x.id);
                    table.ForeignKey(
                        name: "FK_medication_compliances_dose_schedules_dose_schedule_id",
                        column: x => x.dose_schedule_id,
                        principalTable: "dose_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_dose_schedules_medication_id",
                table: "dose_schedules",
                column: "medication_id");

            migrationBuilder.CreateIndex(
                name: "IX_medication_compliances_dose_schedule_id",
                table: "medication_compliances",
                column: "dose_schedule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_compliances");

            migrationBuilder.DropTable(
                name: "medication_compliances");

            migrationBuilder.DropTable(
                name: "offline_sync_queue");

            migrationBuilder.DropTable(
                name: "dose_schedules");

            migrationBuilder.DropTable(
                name: "medications");
        }
    }
}
