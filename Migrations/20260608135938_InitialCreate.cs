using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MediAlert.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsSuspended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedStripeEvents",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedStripeEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AdminId);
                    table.ForeignKey(
                        name: "FK_Admins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Caregivers",
                columns: table => new
                {
                    CaregiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caregivers", x => x.CaregiverId);
                    table.ForeignKey(
                        name: "FK_Caregivers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Specialization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Qualifications = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.DoctorId);
                    table.ForeignKey(
                        name: "FK_Doctors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "DoctorAvailabilities",
                columns: table => new
                {
                    DoctorAvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilities", x => x.DoctorAvailabilityId);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilities_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingAuditLogs",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingAuditLogs", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_BillingAuditLogs_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaregiverPatientLinks",
                columns: table => new
                {
                    CaregiverPatientLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaregiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaregiverPatientLinks", x => x.CaregiverPatientLinkId);
                    table.ForeignKey(
                        name: "FK_CaregiverPatientLinks_Caregivers_CaregiverId",
                        column: x => x.CaregiverId,
                        principalTable: "Caregivers",
                        principalColumn: "CaregiverId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaregiverPatientLinks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
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
                name: "Consultations",
                columns: table => new
                {
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ZoomMeetingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultations", x => x.ConsultationId);
                    table.ForeignKey(
                        name: "FK_Consultations_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultations_Patients_PatientId",
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
                name: "PatientDoctorLinks",
                columns: table => new
                {
                    PatientDoctorLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDoctorLinks", x => x.PatientDoctorLinkId);
                    table.ForeignKey(
                        name: "FK_PatientDoctorLinks_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientDoctorLinks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    StripePriceId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationNotes",
                columns: table => new
                {
                    ConsultationNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "text", nullable: false),
                    Prescriptions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationNotes", x => x.ConsultationNoteId);
                    table.ForeignKey(
                        name: "FK_ConsultationNotes_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "ConsultationId",
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
                name: "Invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeInvoiceId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HostedInvoiceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoicePdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NextPaymentAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_Invoices_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "SubscriptionId",
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
                name: "IX_Admins_UserId",
                table: "Admins",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsEmailVerified",
                table: "AspNetUsers",
                column: "IsEmailVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "AspNetUsers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingAuditLogs_PatientId",
                table: "BillingAuditLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CaregiverPatientLinks_PatientId",
                table: "CaregiverPatientLinks",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "UX_CaregiverPatientLinks_Caregiver_Patient",
                table: "CaregiverPatientLinks",
                columns: new[] { "CaregiverId", "PatientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Caregivers_UserId",
                table: "Caregivers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ComplianceReports_PatientId_Period",
                table: "ComplianceReports",
                columns: new[] { "PatientId", "PeriodStartDate", "PeriodEndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationNotes_ConsultationId",
                table: "ConsultationNotes",
                column: "ConsultationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_Doctor_ScheduledTime",
                table: "Consultations",
                columns: new[] { "DoctorId", "ScheduledDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Consultations_PatientId",
                table: "Consultations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilities_Doctor_DayOfWeek",
                table: "DoctorAvailabilities",
                columns: new[] { "DoctorId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Doctors_LicenseNumber",
                table: "Doctors",
                column: "LicenseNumber",
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
                name: "IX_Invoices_SubscriptionId",
                table: "Invoices",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "UX_Invoices_StripeInvoiceId",
                table: "Invoices",
                column: "StripeInvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Medications_PatientId_IsActive",
                table: "Medications",
                columns: new[] { "PatientId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientDoctorLinks_DoctorId",
                table: "PatientDoctorLinks",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "UX_PatientDoctorLinks_Patient_Doctor",
                table: "PatientDoctorLinks",
                columns: new[] { "PatientId", "DoctorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PatientId",
                table: "Subscriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeCustomerId",
                table: "Subscriptions",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "UX_Subscriptions_StripeSubscriptionId",
                table: "Subscriptions",
                column: "StripeSubscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BillingAuditLogs");

            migrationBuilder.DropTable(
                name: "CaregiverPatientLinks");

            migrationBuilder.DropTable(
                name: "ComplianceReports");

            migrationBuilder.DropTable(
                name: "ConsultationNotes");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilities");

            migrationBuilder.DropTable(
                name: "IntakeLogs");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PatientDoctorLinks");

            migrationBuilder.DropTable(
                name: "ProcessedStripeEvents");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Caregivers");

            migrationBuilder.DropTable(
                name: "Consultations");

            migrationBuilder.DropTable(
                name: "DoseSchedules");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
