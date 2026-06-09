using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediAlert.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractionReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InteractionReports",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryDrugName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExistingDrugNames = table.Column<string>(type: "text", nullable: false),
                    SeverityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExplanationText = table.Column<string>(type: "text", nullable: false),
                    IsSaved = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractionReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_InteractionReports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InteractionReports_PatientId",
                table: "InteractionReports",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InteractionReports");
        }
    }
}
