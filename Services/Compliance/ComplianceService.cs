using MediAlert.Constants;
using MediAlert.DTOs.Compliance;
using MediAlert.DTOs.OpenFda;
using MediAlert.Models;
using MediAlert.Repositories.Compliance.Interfaces;
using MediAlert.Services.Compliance.Interfaces;
using MediAlert.Services.OpenFda.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediAlert.Services.Compliance;

public sealed class ComplianceService : IComplianceService
{
    private const int MaxReportDays = 366;
    private const int MaxSafetySummaryMedications = 10;

    private readonly IComplianceRepository _repository;
    private readonly IOpenFdaDrugClient _openFdaDrugClient;
    private readonly ILogger<ComplianceService> _logger;

    public ComplianceService(
        IComplianceRepository repository,
        IOpenFdaDrugClient openFdaDrugClient,
        ILogger<ComplianceService> logger)
    {
        _repository = repository;
        _openFdaDrugClient = openFdaDrugClient;
        _logger = logger;
    }

    public async Task<ComplianceServiceResult<IntakeLogResponse>> RecordIntakeAsync(
        RecordIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateRecordIntakeRequest(request);
        if (validationErrors.Count > 0)
        {
            return ComplianceServiceResult<IntakeLogResponse>.ValidationFailure(validationErrors);
        }

        var patient = await _repository.GetPatientAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return ComplianceServiceResult<IntakeLogResponse>.Failure(
                ComplianceErrorCodes.PatientNotFound,
                "Patient was not found.",
                StatusCodes.Status404NotFound);
        }

        var doseSchedule = await _repository.GetDoseScheduleForPatientAsync(
            request.PatientId,
            request.DoseScheduleId,
            cancellationToken);

        if (doseSchedule?.Medication is null)
        {
            return ComplianceServiceResult<IntakeLogResponse>.Failure(
                ComplianceErrorCodes.DoseScheduleNotFound,
                "Dose schedule was not found for this patient.",
                StatusCodes.Status404NotFound);
        }

        var normalizedStatus = NormalizeStatus(request.Status);
        var existingLog = await _repository.GetIntakeLogAsync(
            request.PatientId,
            request.DoseScheduleId,
            request.ScheduledDate,
            cancellationToken);

        var log = existingLog ?? new IntakeLog
        {
            IntakeLogId = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoseScheduleId = request.DoseScheduleId,
            ScheduledDate = request.ScheduledDate,
            ScheduledTime = doseSchedule.ScheduledTime,
        };

        log.Status = normalizedStatus;
        log.ActualTakenAt = GetActualTakenAt(normalizedStatus, request.ActualTakenAt);
        log.SkippedReason = string.IsNullOrWhiteSpace(request.SkippedReason)
            ? null
            : request.SkippedReason.Trim();
        log.Notes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();
        log.LoggedAt = DateTime.UtcNow;

        if (existingLog is null)
        {
            await _repository.AddIntakeLogAsync(log, cancellationToken);
        }

        await RefreshPatientStreakAsync(patient, cancellationToken);

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Failed to save intake log for PatientId {PatientId}, DoseScheduleId {DoseScheduleId}, ScheduledDate {ScheduledDate}.",
                request.PatientId,
                request.DoseScheduleId,
                request.ScheduledDate);

            return ComplianceServiceResult<IntakeLogResponse>.Failure(
                ComplianceErrorCodes.SaveFailed,
                "Failed to save intake log.",
                StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation(
            "Recorded intake status {Status} for PatientId {PatientId}, DoseScheduleId {DoseScheduleId}, ScheduledDate {ScheduledDate}.",
            normalizedStatus,
            request.PatientId,
            request.DoseScheduleId,
            request.ScheduledDate);

        return ComplianceServiceResult<IntakeLogResponse>.Success(
            MapIntakeLogResponse(log, doseSchedule.Medication),
            existingLog is null ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }

    public async Task<ComplianceServiceResult<ComplianceHistoryResponse>> GetHistoryAsync(
        ComplianceHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateHistoryRequest(request);
        if (validationErrors.Count > 0)
        {
            return ComplianceServiceResult<ComplianceHistoryResponse>.ValidationFailure(validationErrors);
        }

        var patient = await _repository.GetPatientAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return ComplianceServiceResult<ComplianceHistoryResponse>.Failure(
                ComplianceErrorCodes.PatientNotFound,
                "Patient was not found.",
                StatusCodes.Status404NotFound);
        }

        var normalizedStatus = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : NormalizeStatus(request.Status);

        var logs = await _repository.GetIntakeLogsForPeriodAsync(
            request.PatientId,
            request.FromDate,
            request.ToDate,
            request.MedicationId,
            normalizedStatus,
            cancellationToken);

        var response = new ComplianceHistoryResponse
        {
            PatientId = request.PatientId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalLogs = logs.Count,
            Logs = logs
                .Where(log => log.DoseSchedule?.Medication is not null)
                .Select(log => MapIntakeLogResponse(log, log.DoseSchedule.Medication))
                .ToList(),
        };

        return ComplianceServiceResult<ComplianceHistoryResponse>.Success(response);
    }

    public async Task<ComplianceServiceResult<ComplianceReportResponse>> GenerateReportAsync(
        GenerateComplianceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateReportRequest(request);
        if (validationErrors.Count > 0)
        {
            return ComplianceServiceResult<ComplianceReportResponse>.ValidationFailure(validationErrors);
        }

        var patient = await _repository.GetPatientAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return ComplianceServiceResult<ComplianceReportResponse>.Failure(
                ComplianceErrorCodes.PatientNotFound,
                "Patient was not found.",
                StatusCodes.Status404NotFound);
        }

        var medications = await _repository.GetMedicationsWithSchedulesForPeriodAsync(
            request.PatientId,
            request.PeriodStartDate,
            request.PeriodEndDate,
            cancellationToken);

        var expectedDoses = BuildExpectedDoses(
            medications,
            request.PeriodStartDate,
            request.PeriodEndDate);

        var logs = await _repository.GetIntakeLogsForPeriodAsync(
            request.PatientId,
            request.PeriodStartDate,
            request.PeriodEndDate,
            cancellationToken: cancellationToken);

        var calculation = CalculateReport(expectedDoses, logs);

        if (calculation.TotalScheduledDoses == 0)
        {
            return ComplianceServiceResult<ComplianceReportResponse>.Failure(
                ComplianceErrorCodes.NoScheduledDoses,
                "No scheduled doses were found for this report period.",
                StatusCodes.Status404NotFound);
        }

        var report = await UpsertReportAsync(request, calculation, cancellationToken);
        var response = MapReportResponse(report, request, calculation);

        if (request.IncludeOpenFdaSafetySummary)
        {
            response.OpenFdaSafetySummaries = await BuildOpenFdaSafetySummariesAsync(
                medications,
                cancellationToken);
        }

        try
        {
            if (request.PersistReport)
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Failed to save compliance report for PatientId {PatientId}, Period {StartDate} to {EndDate}.",
                request.PatientId,
                request.PeriodStartDate,
                request.PeriodEndDate);

            return ComplianceServiceResult<ComplianceReportResponse>.Failure(
                ComplianceErrorCodes.SaveFailed,
                "Failed to save compliance report.",
                StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation(
            "Generated compliance report for PatientId {PatientId}, Period {StartDate} to {EndDate}, Compliance {CompliancePercentage}%.",
            request.PatientId,
            request.PeriodStartDate,
            request.PeriodEndDate,
            response.OverallCompliancePercentage);

        return ComplianceServiceResult<ComplianceReportResponse>.Success(response);
    }

    private async Task<ComplianceReport> UpsertReportAsync(
        GenerateComplianceReportRequest request,
        ReportCalculation calculation,
        CancellationToken cancellationToken)
    {
        var existingReport = request.PersistReport
            ? await _repository.GetComplianceReportAsync(
                request.PatientId,
                request.PeriodStartDate,
                request.PeriodEndDate,
                cancellationToken)
            : null;

        var report = existingReport ?? new ComplianceReport
        {
            ComplianceReportId = Guid.NewGuid(),
            PatientId = request.PatientId,
            PeriodStartDate = request.PeriodStartDate,
            PeriodEndDate = request.PeriodEndDate,
        };

        report.TotalScheduledDoses = calculation.TotalScheduledDoses;
        report.TakenDoses = calculation.TakenDoses;
        report.SkippedDoses = calculation.SkippedDoses;
        report.MissedDoses = calculation.MissedDoses;
        report.DelayedDoses = calculation.DelayedDoses;
        report.OverallCompliancePercentage = calculation.OverallCompliancePercentage;
        report.Recommendations = BuildRecommendation(calculation.OverallCompliancePercentage);
        report.GeneratedAt = DateTime.UtcNow;

        if (request.PersistReport && existingReport is null)
        {
            await _repository.AddComplianceReportAsync(report, cancellationToken);
        }

        return report;
    }

    private async Task RefreshPatientStreakAsync(
        Patient patient,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-30);

        var medications = await _repository.GetMedicationsWithSchedulesForPeriodAsync(
            patient.PatientId,
            startDate,
            today,
            cancellationToken);

        var expectedDoses = BuildExpectedDoses(medications, startDate, today);
        var logs = await _repository.GetIntakeLogsForPeriodAsync(
            patient.PatientId,
            startDate,
            today,
            cancellationToken: cancellationToken);

        patient.ComplianceStreakDays = CalculateCurrentStreak(expectedDoses, logs, today);
    }

    private static ReportCalculation CalculateReport(
        IReadOnlyList<ExpectedDose> expectedDoses,
        IReadOnlyList<IntakeLog> logs)
    {
        var logsByScheduleDate = logs
            .GroupBy(log => new DoseKey(log.DoseScheduleId, log.ScheduledDate))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(log => log.LoggedAt).First());

        var medicationSummaries = expectedDoses
            .GroupBy(dose => new { dose.MedicationId, dose.DrugName })
            .Select(group =>
            {
                var statuses = group
                    .Select(dose => GetEffectiveStatus(dose, logsByScheduleDate))
                    .ToList();

                return new MedicationComplianceSummaryResponse
                {
                    MedicationId = group.Key.MedicationId,
                    DrugName = group.Key.DrugName,
                    TotalScheduledDoses = statuses.Count,
                    TakenDoses = statuses.Count(status => status == IntakeStatuses.Taken),
                    SkippedDoses = statuses.Count(status => status == IntakeStatuses.Skipped),
                    MissedDoses = statuses.Count(status => status == IntakeStatuses.Missed),
                    DelayedDoses = statuses.Count(status => status == IntakeStatuses.Delayed),
                    CompliancePercentage = CalculateCompliancePercentage(statuses),
                };
            })
            .OrderBy(summary => summary.DrugName)
            .ToList();

        var allStatuses = expectedDoses
            .Select(dose => GetEffectiveStatus(dose, logsByScheduleDate))
            .ToList();

        return new ReportCalculation
        {
            TotalScheduledDoses = allStatuses.Count,
            TakenDoses = allStatuses.Count(status => status == IntakeStatuses.Taken),
            SkippedDoses = allStatuses.Count(status => status == IntakeStatuses.Skipped),
            MissedDoses = allStatuses.Count(status => status == IntakeStatuses.Missed),
            DelayedDoses = allStatuses.Count(status => status == IntakeStatuses.Delayed),
            OverallCompliancePercentage = CalculateCompliancePercentage(allStatuses),
            MedicationSummaries = medicationSummaries,
        };
    }

    private static int CalculateCurrentStreak(
        IReadOnlyList<ExpectedDose> expectedDoses,
        IReadOnlyList<IntakeLog> logs,
        DateOnly endDate)
    {
        var logsByScheduleDate = logs
            .GroupBy(log => new DoseKey(log.DoseScheduleId, log.ScheduledDate))
            .ToDictionary(group => group.Key, group => group.OrderByDescending(log => log.LoggedAt).First());

        var dosesByDate = expectedDoses
            .GroupBy(dose => dose.ScheduledDate)
            .ToDictionary(group => group.Key, group => group.ToList());

        var streak = 0;

        for (var date = endDate; date >= endDate.AddDays(-30); date = date.AddDays(-1))
        {
            if (!dosesByDate.TryGetValue(date, out var dayDoses) || dayDoses.Count == 0)
            {
                continue;
            }

            var statuses = dayDoses
                .Select(dose => GetEffectiveStatus(dose, logsByScheduleDate))
                .ToList();

            if (statuses.All(status => status is IntakeStatuses.Taken or IntakeStatuses.Delayed))
            {
                streak++;
                continue;
            }

            break;
        }

        return streak;
    }

    private static decimal CalculateCompliancePercentage(IReadOnlyList<string> statuses)
    {
        if (statuses.Count == 0)
        {
            return 0;
        }

        var weightedCompliantDoses = statuses.Sum(status => status switch
        {
            IntakeStatuses.Taken => 1.0m,
            IntakeStatuses.Delayed => 1.0m,
            IntakeStatuses.Skipped => 0.5m,
            _ => 0m,
        });

        return Math.Round(weightedCompliantDoses / statuses.Count * 100, 2);
    }

    private static string GetEffectiveStatus(
        ExpectedDose dose,
        IReadOnlyDictionary<DoseKey, IntakeLog> logsByScheduleDate)
    {
        if (logsByScheduleDate.TryGetValue(
            new DoseKey(dose.DoseScheduleId, dose.ScheduledDate),
            out var log))
        {
            return NormalizeStatus(log.Status);
        }

        return IntakeStatuses.Missed;
    }

    private static List<ExpectedDose> BuildExpectedDoses(
        IReadOnlyList<Medication> medications,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var expectedDoses = new List<ExpectedDose>();

        foreach (var medication in medications)
        {
            var medicationStart = medication.StartDate > fromDate
                ? medication.StartDate
                : fromDate;

            var medicationEnd = medication.EndDate.HasValue && medication.EndDate.Value < toDate
                ? medication.EndDate.Value
                : toDate;

            if (medicationEnd < medicationStart)
            {
                continue;
            }

            foreach (var schedule in medication.DoseSchedules.Where(schedule => schedule.IsActive))
            {
                for (var date = medicationStart; date <= medicationEnd; date = date.AddDays(1))
                {
                    if (schedule.DayOfWeek is not null && schedule.DayOfWeek.Value != (int)date.DayOfWeek)
                    {
                        continue;
                    }

                    expectedDoses.Add(new ExpectedDose(
                        medication.MedicationId,
                        medication.DrugName,
                        schedule.DoseScheduleId,
                        date,
                        schedule.ScheduledTime));
                }
            }
        }

        return expectedDoses;
    }

    private async Task<List<MedicationSafetySummaryResponse>> BuildOpenFdaSafetySummariesAsync(
        IReadOnlyList<Medication> medications,
        CancellationToken cancellationToken)
    {
        var summaries = new List<MedicationSafetySummaryResponse>();

        foreach (var medication in medications
            .Where(medication => !string.IsNullOrWhiteSpace(medication.DrugName))
            .GroupBy(medication => medication.DrugName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(MaxSafetySummaryMedications))
        {
            var result = await _openFdaDrugClient.SearchDrugLabelsAsync(
                new OpenFdaDrugSearchRequest
                {
                    Query = medication.DrugName,
                    Limit = 1,
                },
                cancellationToken);

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "OpenFDA safety summary failed for MedicationId {MedicationId}, DrugName {DrugName}: {ErrorCode} {ErrorMessage}",
                    medication.MedicationId,
                    medication.DrugName,
                    result.ErrorCode,
                    result.ErrorMessage);

                summaries.Add(new MedicationSafetySummaryResponse
                {
                    MedicationId = medication.MedicationId,
                    DrugName = medication.DrugName,
                    RetrievedFromOpenFda = false,
                    ErrorMessage = result.ErrorMessage,
                });

                continue;
            }

            var match = result.Data?.Results.FirstOrDefault();

            summaries.Add(new MedicationSafetySummaryResponse
            {
                MedicationId = medication.MedicationId,
                DrugName = medication.DrugName,
                RetrievedFromOpenFda = match is not null,
                ErrorMessage = match is null ? "No OpenFDA drug label result was found." : null,
                BrandNames = match?.BrandNames ?? [],
                GenericNames = match?.GenericNames ?? [],
                Warnings = TakeTop(match?.Warnings),
                BoxedWarnings = TakeTop(match?.BoxedWarnings),
                Contraindications = TakeTop(match?.Contraindications),
                DrugInteractions = TakeTop(match?.DrugInteractions),
            });
        }

        return summaries;
    }

    private static ComplianceReportResponse MapReportResponse(
        ComplianceReport report,
        GenerateComplianceReportRequest request,
        ReportCalculation calculation) =>
        new()
        {
            ComplianceReportId = request.PersistReport ? report.ComplianceReportId : null,
            PatientId = request.PatientId,
            PeriodStartDate = request.PeriodStartDate,
            PeriodEndDate = request.PeriodEndDate,
            TotalScheduledDoses = calculation.TotalScheduledDoses,
            TakenDoses = calculation.TakenDoses,
            SkippedDoses = calculation.SkippedDoses,
            MissedDoses = calculation.MissedDoses,
            DelayedDoses = calculation.DelayedDoses,
            OverallCompliancePercentage = calculation.OverallCompliancePercentage,
            Recommendations = report.Recommendations,
            GeneratedAt = report.GeneratedAt,
            MedicationSummaries = calculation.MedicationSummaries,
        };

    private static IntakeLogResponse MapIntakeLogResponse(
        IntakeLog log,
        Medication medication) =>
        new()
        {
            IntakeLogId = log.IntakeLogId,
            PatientId = log.PatientId,
            MedicationId = medication.MedicationId,
            MedicationName = medication.DrugName,
            DoseScheduleId = log.DoseScheduleId,
            ScheduledDate = log.ScheduledDate,
            ScheduledTime = log.ScheduledTime,
            Status = log.Status,
            ActualTakenAt = log.ActualTakenAt,
            LoggedAt = log.LoggedAt,
            SkippedReason = log.SkippedReason,
            Notes = log.Notes,
        };

    private static Dictionary<string, string[]> ValidateRecordIntakeRequest(RecordIntakeRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.PatientId == Guid.Empty)
        {
            errors[nameof(request.PatientId)] = ["PatientId is required."];
        }

        if (request.DoseScheduleId == Guid.Empty)
        {
            errors[nameof(request.DoseScheduleId)] = ["DoseScheduleId is required."];
        }

        if (request.ScheduledDate == default)
        {
            errors[nameof(request.ScheduledDate)] = ["ScheduledDate is required."];
        }

        if (!IntakeStatuses.IsValid(request.Status))
        {
            errors[nameof(request.Status)] = [$"Status must be one of: {string.Join(", ", IntakeStatuses.All)}."];
        }

        if (request.SkippedReason?.Length > 250)
        {
            errors[nameof(request.SkippedReason)] = ["SkippedReason must not exceed 250 characters."];
        }

        if (request.Notes?.Length > 500)
        {
            errors[nameof(request.Notes)] = ["Notes must not exceed 500 characters."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateHistoryRequest(ComplianceHistoryRequest request)
    {
        var errors = ValidateDateRange(request.PatientId, request.FromDate, request.ToDate);

        if (!string.IsNullOrWhiteSpace(request.Status) && !IntakeStatuses.IsValid(request.Status))
        {
            errors[nameof(request.Status)] = [$"Status must be one of: {string.Join(", ", IntakeStatuses.All)}."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateReportRequest(GenerateComplianceReportRequest request) =>
        ValidateDateRange(request.PatientId, request.PeriodStartDate, request.PeriodEndDate);

    private static Dictionary<string, string[]> ValidateDateRange(
        Guid patientId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var errors = new Dictionary<string, string[]>();

        if (patientId == Guid.Empty)
        {
            errors["PatientId"] = ["PatientId is required."];
        }

        if (startDate == default)
        {
            errors["StartDate"] = ["Start date is required."];
        }

        if (endDate == default)
        {
            errors["EndDate"] = ["End date is required."];
        }

        if (startDate != default && endDate != default && endDate < startDate)
        {
            errors["DateRange"] = ["End date must be on or after start date."];
        }

        if (startDate != default && endDate != default && endDate.DayNumber - startDate.DayNumber > MaxReportDays)
        {
            errors["DateRange"] = [$"Date range cannot exceed {MaxReportDays} days."];
        }

        return errors;
    }

    private static string NormalizeStatus(string status)
    {
        var matchedStatus = IntakeStatuses.All.FirstOrDefault(
            validStatus => string.Equals(validStatus, status.Trim(), StringComparison.OrdinalIgnoreCase));

        return matchedStatus ?? status.Trim();
    }

    private static DateTime? GetActualTakenAt(string status, DateTime? actualTakenAt)
    {
        if (status is IntakeStatuses.Taken or IntakeStatuses.Delayed)
        {
            return actualTakenAt ?? DateTime.UtcNow;
        }

        return null;
    }

    private static string BuildRecommendation(decimal compliancePercentage) =>
        compliancePercentage switch
        {
            >= 90 => "Excellent adherence. Continue the current medication routine.",
            >= 75 => "Good adherence. Review delayed or skipped doses to improve consistency.",
            >= 50 => "Moderate adherence. Consider reminders or caregiver support.",
            _ => "Low adherence. Follow up with a healthcare professional or caregiver support plan.",
        };

    private static List<string> TakeTop(IEnumerable<string>? values) =>
        values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList()
        ?? [];

    private sealed record DoseKey(Guid DoseScheduleId, DateOnly ScheduledDate);

    private sealed record ExpectedDose(
        Guid MedicationId,
        string DrugName,
        Guid DoseScheduleId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledTime);

    private sealed class ReportCalculation
    {
        public int TotalScheduledDoses { get; init; }
        public int TakenDoses { get; init; }
        public int SkippedDoses { get; init; }
        public int MissedDoses { get; init; }
        public int DelayedDoses { get; init; }
        public decimal OverallCompliancePercentage { get; init; }
        public List<MedicationComplianceSummaryResponse> MedicationSummaries { get; init; } = [];
    }
}
