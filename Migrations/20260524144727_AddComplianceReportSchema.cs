using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediAlert.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceReportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ComplianceStreakDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.CheckConstraint("CK_Patients_ComplianceStreakDays_NonNegative", "\"ComplianceStreakDays\" >= 0");
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceReports",
                columns: table => new
                {
                    ComplianceReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalScheduledDoses = table.Column<int>(type: "integer", nullable: false),
                    TakenDoses = table.Column<int>(type: "integer", nullable: false),
                    SkippedDoses = table.Column<int>(type: "integer", nullable: false),
                    MissedDoses = table.Column<int>(type: "integer", nullable: false),
                    DelayedDoses = table.Column<int>(type: "integer", nullable: false),
                    OverallCompliancePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Recommendations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceReports", x => x.ComplianceReportId);
                    table.CheckConstraint("CK_ComplianceReports_DateRange", "\"PeriodEndDate\" >= \"PeriodStartDate\"");
                    table.CheckConstraint("CK_ComplianceReports_DoseCounts_NonNegative", "\"TakenDoses\" >= 0 AND \"SkippedDoses\" >= 0 AND \"MissedDoses\" >= 0 AND \"DelayedDoses\" >= 0");
                    table.CheckConstraint("CK_ComplianceReports_DoseCounts_NotOverTotal", "(\"TakenDoses\" + \"SkippedDoses\" + \"MissedDoses\" + \"DelayedDoses\") <= \"TotalScheduledDoses\"");
                    table.CheckConstraint("CK_ComplianceReports_OverallCompliancePercentage_Range", "\"OverallCompliancePercentage\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_ComplianceReports_TotalScheduledDoses_NonNegative", "\"TotalScheduledDoses\" >= 0");
                    table.ForeignKey(
                        name: "FK_ComplianceReports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    MedicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DosageStrength = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DosageForm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FrequencyPerDay = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PrescribingPhysician = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PharmacyName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.MedicationId);
                    table.CheckConstraint("CK_Medications_DateRange", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("CK_Medications_FrequencyPerDay_Range", "\"FrequencyPerDay\" BETWEEN 1 AND 24");
                    table.ForeignKey(
                        name: "FK_Medications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoseSchedules",
                columns: table => new
                {
                    DoseScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoseSchedules", x => x.DoseScheduleId);
                    table.CheckConstraint("CK_DoseSchedules_DayOfWeek_Range", "\"DayOfWeek\" IS NULL OR \"DayOfWeek\" BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_DoseSchedules_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "MedicationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntakeLogs",
                columns: table => new
                {
                    IntakeLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoseScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActualTakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SkippedReason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeLogs", x => x.IntakeLogId);
                    table.CheckConstraint("CK_IntakeLogs_Status", "\"Status\" IN ('Taken', 'Skipped', 'Missed', 'Delayed')");
                    table.ForeignKey(
                        name: "FK_IntakeLogs_DoseSchedules_DoseScheduleId",
                        column: x => x.DoseScheduleId,
                        principalTable: "DoseSchedules",
                        principalColumn: "DoseScheduleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntakeLogs_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ComplianceReports_PatientId_Period",
                table: "ComplianceReports",
                columns: new[] { "PatientId", "PeriodStartDate", "PeriodEndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoseSchedules_MedicationId_IsActive",
                table: "DoseSchedules",
                columns: new[] { "MedicationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeLogs_PatientId_ScheduledDate",
                table: "IntakeLogs",
                columns: new[] { "PatientId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeLogs_Status",
                table: "IntakeLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_IntakeLogs_DoseScheduleId_ScheduledDate",
                table: "IntakeLogs",
                columns: new[] { "DoseScheduleId", "ScheduledDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_PatientId_IsActive",
                table: "Medications",
                columns: new[] { "PatientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceReports");

            migrationBuilder.DropTable(
                name: "IntakeLogs");

            migrationBuilder.DropTable(
                name: "DoseSchedules");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}
