using MediAlert.DTOs.Compliance;
using MediAlert.Models;
using MediAlert.Services.Reports.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace MediAlert.Services.Reports;

public sealed class ComplianceReportPdfExportService : IPdfExportService
{
    public Task<byte[]> GenerateComplianceReportPdfAsync(ComplianceReportResponse reportData, Patient patient)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(x => ComposeHeader(x, patient, reportData));
                page.Content().Element(x => ComposeContent(x, reportData));
                page.Footer().Element(ComposeFooter);
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private void ComposeHeader(IContainer container, Patient patient, ComplianceReportResponse reportData)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("MediAlert").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Compliance Report").FontSize(16).FontColor(Colors.Grey.Darken1);
                
                column.Item().PaddingTop(10).Text(text =>
                {
                    text.Span("Patient: ").SemiBold();
                    text.Span(patient.User?.FullName ?? "Unknown");
                });
                column.Item().Text(text =>
                {
                    text.Span("Period: ").SemiBold();
                    text.Span($"{reportData.PeriodStartDate:MMM dd, yyyy} - {reportData.PeriodEndDate:MMM dd, yyyy}");
                });
            });

            row.ConstantItem(100).Height(50).Placeholder(); // Placeholder for logo if needed
        });
    }

    private void ComposeContent(IContainer container, ComplianceReportResponse reportData)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(20);

            // Summary Section
            column.Item().Element(x => ComposeSummarySection(x, reportData));

            // Medication Breakdown Table
            column.Item().Element(x => ComposeMedicationTable(x, reportData));

            // Trends
            column.Item().Element(x => ComposeTrendsChart(x, reportData));

            // Recommendations
            column.Item().Element(x => ComposeRecommendations(x, reportData));
        });
    }

    private void ComposeSummarySection(IContainer container, ComplianceReportResponse reportData)
    {
        container.Background(Colors.Grey.Lighten4).Padding(10).Column(column =>
        {
            column.Spacing(5);
            column.Item().Text("Overall Summary").FontSize(14).SemiBold();
            
            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Overall Compliance: {reportData.OverallCompliancePercentage}%").FontSize(12).SemiBold()
                    .FontColor(reportData.OverallCompliancePercentage >= 80m ? Colors.Green.Medium : Colors.Red.Medium);
                
                row.RelativeItem().Text($"Scheduled Doses: {reportData.TotalScheduledDoses}");
                row.RelativeItem().Text($"Taken: {reportData.TakenDoses}");
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Missed: {reportData.MissedDoses}").FontColor(Colors.Red.Medium);
                row.RelativeItem().Text($"Skipped: {reportData.SkippedDoses}").FontColor(Colors.Orange.Medium);
                row.RelativeItem().Text($"Delayed: {reportData.DelayedDoses}").FontColor(Colors.Blue.Medium);
            });
        });
    }

    private void ComposeMedicationTable(IContainer container, ComplianceReportResponse reportData)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("Medication Breakdown").FontSize(14).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Drug Name
                    columns.RelativeColumn(1); // Scheduled
                    columns.RelativeColumn(1); // Taken
                    columns.RelativeColumn(1); // Missed
                    columns.RelativeColumn(1); // %
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Drug Name");
                    header.Cell().Element(CellStyle).AlignRight().Text("Scheduled");
                    header.Cell().Element(CellStyle).AlignRight().Text("Taken");
                    header.Cell().Element(CellStyle).AlignRight().Text("Missed");
                    header.Cell().Element(CellStyle).AlignRight().Text("Compliance");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                foreach (var med in reportData.MedicationSummaries)
                {
                    table.Cell().Element(CellStyle).Text(med.DrugName);
                    table.Cell().Element(CellStyle).AlignRight().Text(med.TotalScheduledDoses.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(med.TakenDoses.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(med.MissedDoses.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text($"{med.CompliancePercentage}%")
                        .FontColor(med.CompliancePercentage >= 80m ? Colors.Green.Medium : Colors.Red.Medium);

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                }
            });
        });
    }

    private void ComposeTrendsChart(IContainer container, ComplianceReportResponse reportData)
    {
        if (reportData.Trends == null || !reportData.Trends.Any())
            return;

        container.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("Daily Compliance Trend (Visualized)").FontSize(14).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn();   // Bar
                    columns.ConstantColumn(50); // Value
                });

                foreach (var trend in reportData.Trends)
                {
                    table.Cell().PaddingVertical(2).Text(trend.Date.ToString("MMM dd")).FontSize(10);
                    
                    table.Cell().PaddingVertical(2).AlignMiddle().Row(row =>
                    {
                        var widthPercentage = (float)trend.CompliancePercentage / 100f;
                        var barColor = trend.CompliancePercentage >= 80m ? Colors.Green.Medium : 
                                       trend.CompliancePercentage >= 50m ? Colors.Orange.Medium : Colors.Red.Medium;

                        // Draw the filled part
                        if (widthPercentage > 0f)
                        {
                            row.RelativeItem(widthPercentage).Height(10).Background(barColor);
                        }
                        
                        // Draw the empty part
                        if (widthPercentage < 1f)
                        {
                            row.RelativeItem(1f - widthPercentage).Height(10).Background(Colors.Grey.Lighten3);
                        }
                    });

                    table.Cell().PaddingVertical(2).AlignRight().Text($"{trend.CompliancePercentage}%").FontSize(10);
                }
            });
        });
    }

    private void ComposeRecommendations(IContainer container, ComplianceReportResponse reportData)
    {
        if (string.IsNullOrWhiteSpace(reportData.Recommendations))
            return;

        container.Background(Colors.Blue.Lighten5).Padding(10).Column(column =>
        {
            column.Item().Text("Recommendations").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(5).Text(reportData.Recommendations);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }
}
