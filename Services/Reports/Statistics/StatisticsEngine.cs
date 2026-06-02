using MediAlert.Constants;
using MediAlert.DTOs.Compliance;
using MediAlert.Models;

namespace MediAlert.Services.Reports.Statistics;

public sealed class StatisticsEngine : IStatisticsEngine
{
    public ComplianceReportResponse CalculateCompliance(
        Guid patientId, 
        DateOnly startDate, 
        DateOnly endDate, 
        List<IntakeLog> logs, 
        List<Medication> medications)
    {
        var report = new ComplianceReportResponse
        {
            PatientId = patientId,
            PeriodStartDate = startDate,
            PeriodEndDate = endDate,
            GeneratedAt = DateTime.UtcNow,
            TotalScheduledDoses = logs.Count,
            TakenDoses = logs.Count(l => l.Status == IntakeStatuses.Taken),
            DelayedDoses = logs.Count(l => l.Status == IntakeStatuses.Delayed),
            SkippedDoses = logs.Count(l => l.Status == IntakeStatuses.Skipped),
            MissedDoses = logs.Count(l => l.Status == IntakeStatuses.Missed)
        };

        // Calculate Overall Percentage
        // Weighted: Taken = 1.0, Delayed = 1.0, Skipped = 0.5, Missed = 0.0
        // (Assuming standard from ComplianceService)
        decimal totalScore = 0m;
        foreach (var log in logs)
        {
            if (log.Status == IntakeStatuses.Taken || log.Status == IntakeStatuses.Delayed)
                totalScore += 1.0m;
            else if (log.Status == IntakeStatuses.Skipped)
                totalScore += 0.5m;
        }

        report.OverallCompliancePercentage = logs.Count > 0 
            ? Math.Round((totalScore / logs.Count) * 100m, 2) 
            : 0m;

        // Calculate Per Medication Adherence
        var medicationSummaries = new List<MedicationComplianceSummaryResponse>();
        var logsByMedication = logs.GroupBy(l => l.DoseSchedule.MedicationId);

        foreach (var group in logsByMedication)
        {
            var medId = group.Key;
            var med = medications.FirstOrDefault(m => m.MedicationId == medId);
            if (med == null) continue;

            var medLogs = group.ToList();
            var medTotalScore = 0m;
            foreach (var log in medLogs)
            {
                if (log.Status == IntakeStatuses.Taken || log.Status == IntakeStatuses.Delayed)
                    medTotalScore += 1.0m;
                else if (log.Status == IntakeStatuses.Skipped)
                    medTotalScore += 0.5m;
            }

            medicationSummaries.Add(new MedicationComplianceSummaryResponse
            {
                MedicationId = medId,
                DrugName = med.DrugName,
                TotalScheduledDoses = medLogs.Count,
                TakenDoses = medLogs.Count(l => l.Status == IntakeStatuses.Taken),
                SkippedDoses = medLogs.Count(l => l.Status == IntakeStatuses.Skipped),
                MissedDoses = medLogs.Count(l => l.Status == IntakeStatuses.Missed),
                DelayedDoses = medLogs.Count(l => l.Status == IntakeStatuses.Delayed),
                CompliancePercentage = medLogs.Count > 0 
                    ? Math.Round((medTotalScore / medLogs.Count) * 100m, 2) 
                    : 0m
            });
        }
        report.MedicationSummaries = medicationSummaries;

        // Calculate Trends (Daily)
        var trends = new List<DailyTrendResponse>();
        var logsByDate = logs.GroupBy(l => l.ScheduledDate);
        
        // Ensure all dates in range are covered
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dailyLogs = logsByDate.FirstOrDefault(g => g.Key == date)?.ToList() ?? new List<IntakeLog>();
            var dailyTotalScore = 0m;
            foreach (var log in dailyLogs)
            {
                if (log.Status == IntakeStatuses.Taken || log.Status == IntakeStatuses.Delayed)
                    dailyTotalScore += 1.0m;
                else if (log.Status == IntakeStatuses.Skipped)
                    dailyTotalScore += 0.5m;
            }

            trends.Add(new DailyTrendResponse
            {
                Date = date,
                TotalScheduled = dailyLogs.Count,
                TakenDoses = dailyLogs.Count(l => l.Status == IntakeStatuses.Taken),
                MissedDoses = dailyLogs.Count(l => l.Status == IntakeStatuses.Missed),
                CompliancePercentage = dailyLogs.Count > 0 
                    ? Math.Round((dailyTotalScore / dailyLogs.Count) * 100m, 2) 
                    : 0m
            });
        }
        report.Trends = trends.OrderBy(t => t.Date).ToList();

        // Generate Recommendations
        report.Recommendations = GenerateRecommendations(report.OverallCompliancePercentage, report.MedicationSummaries);

        return report;
    }

    private string GenerateRecommendations(decimal overallCompliance, List<MedicationComplianceSummaryResponse> medicationSummaries)
    {
        var recommendations = new List<string>();

        if (overallCompliance >= 90m)
        {
            recommendations.Add("Excellent adherence. Keep up the good work!");
        }
        else if (overallCompliance >= 75m)
        {
            recommendations.Add("Good adherence, but there is room for improvement. Try using alarms to minimize missed doses.");
        }
        else
        {
            recommendations.Add("Compliance is below target. Please review your medication schedule and consider discussing potential barriers with your doctor or pharmacist.");
        }

        var lowestComplianceMed = medicationSummaries.OrderBy(m => m.CompliancePercentage).FirstOrDefault();
        if (lowestComplianceMed != null && lowestComplianceMed.CompliancePercentage < 75m)
        {
            recommendations.Add($"Noticeable difficulty with {lowestComplianceMed.DrugName} ({lowestComplianceMed.CompliancePercentage}%). If side effects or scheduling are an issue, consult your healthcare provider.");
        }

        return string.Join(" ", recommendations);
    }
}
