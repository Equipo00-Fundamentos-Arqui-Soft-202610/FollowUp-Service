using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediTrack.FollowUpService.API.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceValidationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "medication_compliances",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "medication_compliances",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "temporary_video_path",
                table: "medication_compliances",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "validated_at",
                table: "medication_compliances",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "validator_id",
                table: "medication_compliances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "video_expires_at",
                table: "medication_compliances",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "medication_compliances");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "medication_compliances");

            migrationBuilder.DropColumn(
                name: "temporary_video_path",
                table: "medication_compliances");

            migrationBuilder.DropColumn(
                name: "validated_at",
                table: "medication_compliances");

            migrationBuilder.DropColumn(
                name: "validator_id",
                table: "medication_compliances");

            migrationBuilder.DropColumn(
                name: "video_expires_at",
                table: "medication_compliances");
        }
    }
}
