using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediAlert.Migrations
{
    /// <inheritdoc />
    public partial class AddRefillReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefillReminders",
                columns: table => new
                {
                    ReminderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefillReminders", x => x.ReminderId);
                    table.ForeignKey(
                        name: "FK_RefillReminders_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "MedicationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefillReminders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefillReminders_MedicationId",
                table: "RefillReminders",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RefillReminders_Patient_Status",
                table: "RefillReminders",
                columns: new[] { "PatientId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefillReminders");
        }
    }
}
