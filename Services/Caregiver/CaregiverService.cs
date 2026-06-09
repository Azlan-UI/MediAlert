using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.DTOs.Caregiver;
using MediAlert.Models;
using MediAlert.Services.Caregiver.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

namespace MediAlert.Services.Caregiver;

/// <summary>
/// Module 6 — Caregiver Portal.
///
/// All data access goes through ApplicationDbContext directly because this
/// module spans Patient + Caregiver + IntakeLog + DoseSchedule relationships
/// that don't belong to a single bounded repository.  The pattern mirrors
/// ComplianceService which also injects IComplianceRepository (its own repo).
/// We inject ApplicationDbContext via the scoped lifetime — same scope as EF Core.
/// </summary>
public sealed class CaregiverService : ICaregiverService
{
    private const int MaxWindowDays = 30;
    private const int UpcomingDoseCount = 5;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CaregiverService> _logger;

    public CaregiverService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<CaregiverService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FR-14: Patient sends a link request by caregiver email
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> SendLinkRequestAsync(
        Guid patientId,
        CreateCaregiverPatientLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.CaregiverEmail))
            errors[nameof(request.CaregiverEmail)] = ["Caregiver email is required."];
        if (errors.Count > 0)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.ValidationFailure(errors);

        // Resolve patient
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == patientId.ToString(), cancellationToken);

        if (patient is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.PatientNotFound,
                "Patient was not found.",
                StatusCodes.Status404NotFound);

        // Resolve caregiver user by email
        var caregiverUser = await _userManager.FindByEmailAsync(request.CaregiverEmail.Trim());

        if (caregiverUser is null || caregiverUser.Role != UserRoles.Caregiver)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.CaregiverNotFound,
                "No registered caregiver account was found with that email address.",
                StatusCodes.Status404NotFound);

        var caregiver = await _db.Caregivers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == caregiverUser.Id, cancellationToken);

        if (caregiver is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.CaregiverNotFound,
                "Caregiver profile was not found.",
                StatusCodes.Status404NotFound);

        // FR-14: Block duplicate requests
        var existingLink = await _db.CaregiverPatientLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(l =>
                l.CaregiverId == caregiver.CaregiverId &&
                l.PatientId == patientId &&
                l.Status != LinkStatuses.Rejected &&
                l.Status != LinkStatuses.Revoked,
                cancellationToken);

        if (existingLink is not null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkAlreadyExists,
                "A link request to this caregiver is already pending or active.",
                StatusCodes.Status409Conflict);

        var link = new CaregiverPatientLink
        {
            CaregiverPatientLinkId = Guid.NewGuid(),
            CaregiverId            = caregiver.CaregiverId,
            PatientId              = patient.PatientId,
            Status                 = LinkStatuses.Pending,
            CreatedAt              = DateTime.UtcNow,
        };

        await _db.CaregiverPatientLinks.AddAsync(link, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Failed to save caregiver link request PatientId {PatientId} → CaregiverId {CaregiverId}.",
                patientId, caregiver.CaregiverId);
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.SaveFailed,
                "Failed to save link request.",
                StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation(
            "Patient {PatientId} sent caregiver link request to CaregiverId {CaregiverId}.",
            patientId, caregiver.CaregiverId);

        return CaregiverServiceResult<CaregiverPatientLinkResponse>.Success(
            MapLinkResponse(link), StatusCodes.Status201Created);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FR-15: Caregiver approves a pending link
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> ApproveLinkRequestAsync(
        Guid linkId,
        Guid caregiverId,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.CaregiverPatientLinks
            .FirstOrDefaultAsync(l => l.CaregiverPatientLinkId == linkId, cancellationToken);

        if (link is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkNotFound,
                "Link request was not found.",
                StatusCodes.Status404NotFound);

        var caregiver = await _db.Caregivers.FirstOrDefaultAsync(c => c.UserId == caregiverId.ToString(), cancellationToken);
        if (caregiver is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.CaregiverNotFound,
                "Caregiver profile was not found.",
                StatusCodes.Status404NotFound);

        // FR-15: Only the targeted caregiver may approve
        if (link.CaregiverId != caregiver.CaregiverId)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.Unauthorized,
                "You are not authorized to action this link request.",
                StatusCodes.Status403Forbidden);

        if (link.Status != LinkStatuses.Pending)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkAlreadyApproved,
                $"This link request is already '{link.Status}' and cannot be approved.",
                StatusCodes.Status409Conflict);

        link.Status       = LinkStatuses.Approved;
        link.ApprovedDate = DateTime.UtcNow;

        await SaveOrFailAsync(cancellationToken,
            "approve caregiver link", linkId, caregiverId);

        _logger.LogInformation(
            "CaregiverId {CaregiverId} approved link {LinkId} for PatientId {PatientId}.",
            caregiverId, linkId, link.PatientId);

        return CaregiverServiceResult<CaregiverPatientLinkResponse>.Success(MapLinkResponse(link));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FR-15: Caregiver rejects a pending link
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> RejectLinkRequestAsync(
        Guid linkId,
        Guid caregiverId,
        string? rejectionReason,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.CaregiverPatientLinks
            .FirstOrDefaultAsync(l => l.CaregiverPatientLinkId == linkId, cancellationToken);

        if (link is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkNotFound,
                "Link request was not found.",
                StatusCodes.Status404NotFound);

        var caregiver = await _db.Caregivers.FirstOrDefaultAsync(c => c.UserId == caregiverId.ToString(), cancellationToken);
        if (caregiver is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.CaregiverNotFound,
                "Caregiver profile was not found.",
                StatusCodes.Status404NotFound);

        if (link.CaregiverId != caregiver.CaregiverId)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.Unauthorized,
                "You are not authorized to action this link request.",
                StatusCodes.Status403Forbidden);

        if (link.Status != LinkStatuses.Pending)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkAlreadyApproved,
                $"This link request is '{link.Status}' and cannot be rejected.",
                StatusCodes.Status409Conflict);

        link.Status = LinkStatuses.Rejected;

        await SaveOrFailAsync(cancellationToken,
            "reject caregiver link", linkId, caregiverId);

        _logger.LogInformation(
            "CaregiverId {CaregiverId} rejected link {LinkId}.", caregiverId, linkId);

        return CaregiverServiceResult<CaregiverPatientLinkResponse>.Success(MapLinkResponse(link));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Revoke — either patient or caregiver may revoke an Approved link
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> RevokeLinkAsync(
        Guid linkId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.CaregiverPatientLinks
            .Include(l => l.Patient)
            .Include(l => l.Caregiver)
            .FirstOrDefaultAsync(l => l.CaregiverPatientLinkId == linkId, cancellationToken);

        if (link is null)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkNotFound,
                "Link was not found.",
                StatusCodes.Status404NotFound);

        // Only the patient or caregiver on this specific link may revoke
        bool isPatient    = link.Patient?.UserId    == requestingUserId.ToString();
        bool isCaregiver  = link.Caregiver?.UserId  == requestingUserId.ToString();

        if (!isPatient && !isCaregiver)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.Unauthorized,
                "You are not authorized to revoke this link.",
                StatusCodes.Status403Forbidden);

        if (link.Status == LinkStatuses.Revoked)
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.LinkNotApproved,
                "This link is already revoked.",
                StatusCodes.Status409Conflict);

        link.Status = LinkStatuses.Revoked;

        await SaveOrFailAsync(cancellationToken,
            "revoke caregiver link", linkId, requestingUserId);

        _logger.LogInformation(
            "Link {LinkId} revoked by UserId {UserId}.", linkId, requestingUserId);

        return CaregiverServiceResult<CaregiverPatientLinkResponse>.Success(MapLinkResponse(link));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Read all links for a caregiver
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<List<CaregiverPatientLinkResponse>>> GetCaregiverLinksAsync(
        Guid caregiverId,
        CancellationToken cancellationToken = default)
    {
        var links = await _db.CaregiverPatientLinks
            .AsNoTracking()
            .Include(l => l.Patient)
                .ThenInclude(p => p.User)
            .Include(l => l.Caregiver)
                .ThenInclude(c => c.User)
            .Where(l => l.Caregiver.UserId == caregiverId.ToString())
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return CaregiverServiceResult<List<CaregiverPatientLinkResponse>>.Success(
            links.Select(MapLinkResponse).ToList());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Read all links a patient created
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<List<CaregiverPatientLinkResponse>>> GetPatientLinksAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var links = await _db.CaregiverPatientLinks
            .AsNoTracking()
            .Include(l => l.Patient)
                .ThenInclude(p => p.User)
            .Include(l => l.Caregiver)
                .ThenInclude(c => c.User)
            .Where(l => l.Patient.UserId == patientId.ToString())
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return CaregiverServiceResult<List<CaregiverPatientLinkResponse>>.Success(
            links.Select(MapLinkResponse).ToList());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FR-16: Caregiver monitoring dashboard — read-only, gated on Approved link
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<CaregiverServiceResult<CaregiverMonitoringDashboardResponse>> GetPatientDashboardAsync(
        Guid caregiverId,
        Guid patientId,
        int windowDays,
        CancellationToken cancellationToken = default)
    {
        // FR-16: Access blocked without an Approved link
        var link = await _db.CaregiverPatientLinks
            .Include(l => l.Caregiver)
            .AsNoTracking()
            .FirstOrDefaultAsync(l =>
                l.Caregiver.UserId == caregiverId.ToString() &&
                l.PatientId   == patientId   &&
                l.Status      == LinkStatuses.Approved,
                cancellationToken);

        if (link is null)
            return CaregiverServiceResult<CaregiverMonitoringDashboardResponse>.Failure(
                CaregiverErrorCodes.LinkNotApproved,
                "No approved caregiver link exists for this patient.",
                StatusCodes.Status403Forbidden);

        // Clamp window
        windowDays = Math.Clamp(windowDays, 1, MaxWindowDays);

        var patient = await _db.Patients
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == patientId, cancellationToken);

        if (patient is null)
            return CaregiverServiceResult<CaregiverMonitoringDashboardResponse>.Failure(
                CaregiverErrorCodes.PatientNotFound,
                "Patient was not found.",
                StatusCodes.Status404NotFound);

        var today     = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-windowDays + 1);

        // --- Compliance overview ---
        var medications = await _db.Medications
            .Include(m => m.DoseSchedules)
            .AsNoTracking()
            .Where(m =>
                m.PatientId == patientId &&
                m.IsActive  &&
                m.StartDate <= today &&
                (m.EndDate == null || m.EndDate >= startDate))
            .ToListAsync(cancellationToken);

        var logs = await _db.IntakeLogs
            .Include(l => l.DoseSchedule)
                .ThenInclude(ds => ds.Medication)
            .AsNoTracking()
            .Where(l =>
                l.PatientId     == patientId &&
                l.ScheduledDate >= startDate &&
                l.ScheduledDate <= today)
            .ToListAsync(cancellationToken);

        var overview = BuildComplianceOverview(patient, medications, logs, startDate, today, windowDays);

        // --- Missed-dose alerts (last windowDays) ---
        var missedAlerts = logs
            .Where(l => l.Status == IntakeStatuses.Missed &&
                        l.DoseSchedule?.Medication is not null)
            .OrderByDescending(l => l.ScheduledDate)
            .ThenByDescending(l => l.ScheduledTime)
            .Take(20)
            .Select(l => new CaregiverMissedDoseAlertResponse
            {
                IntakeLogId    = l.IntakeLogId,
                PatientId      = l.PatientId,
                MedicationId   = l.DoseSchedule.Medication.MedicationId,
                MedicationName = l.DoseSchedule.Medication.DrugName,
                ScheduledDate  = l.ScheduledDate,
                ScheduledTime  = l.ScheduledTime,
                Status         = l.Status,
                LoggedAt       = l.LoggedAt,
            })
            .ToList();

        // --- Upcoming 5 scheduled doses ---
        var upcomingDoses = await BuildUpcomingDosesAsync(patientId, today, cancellationToken);

        var dashboard = new CaregiverMonitoringDashboardResponse
        {
            PatientId          = patientId,
            PatientFullName    = patient.User.FullName,
            PatientEmail       = patient.User.Email,
            PatientPhoneNumber = patient.PhoneNumber,
            ComplianceOverview = overview,
            MissedDoseAlerts   = missedAlerts,
            UpcomingDoses      = upcomingDoses,
        };

        return CaregiverServiceResult<CaregiverMonitoringDashboardResponse>.Success(dashboard);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static CaregiverComplianceOverviewResponse BuildComplianceOverview(
        Patient patient,
        IReadOnlyList<Medication> medications,
        IReadOnlyList<IntakeLog> logs,
        DateOnly startDate,
        DateOnly endDate,
        int windowDays)
    {
        // Build expected-dose count per medication
        var logsByMed = logs
            .Where(l => l.DoseSchedule?.Medication is not null)
            .GroupBy(l => l.DoseSchedule.Medication.MedicationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<CaregiverMedicationComplianceRowResponse>();
        int totalScheduled = 0;
        int totalTaken     = 0;
        int totalMissed    = 0;

        foreach (var med in medications)
        {
            var medStart = med.StartDate > startDate ? med.StartDate : startDate;
            var medEnd   = med.EndDate.HasValue && med.EndDate.Value < endDate
                           ? med.EndDate.Value : endDate;

            int scheduled = 0;
            foreach (var schedule in med.DoseSchedules.Where(s => s.IsActive))
            {
                for (var d = medStart; d <= medEnd; d = d.AddDays(1))
                {
                    if (schedule.DayOfWeek is null || schedule.DayOfWeek == (int)d.DayOfWeek)
                        scheduled++;
                }
            }

            var medLogs   = logsByMed.TryGetValue(med.MedicationId, out var ml) ? ml : [];
            int taken     = medLogs.Count(l => l.Status is IntakeStatuses.Taken or IntakeStatuses.Delayed);
            int missed    = scheduled - taken;
            int delayed   = medLogs.Count(l => l.Status == IntakeStatuses.Delayed);
            decimal pct   = scheduled > 0 ? Math.Round((decimal)taken / scheduled * 100, 2) : 0;

            rows.Add(new CaregiverMedicationComplianceRowResponse
            {
                MedicationId          = med.MedicationId,
                DrugName              = med.DrugName,
                CompliancePercentage  = pct,
                TakenDoses            = taken,
                MissedDoses           = Math.Max(0, missed),
                DelayedDoses          = delayed,
            });

            totalScheduled += scheduled;
            totalTaken     += taken;
            totalMissed    += Math.Max(0, missed);
        }

        decimal overallPct = totalScheduled > 0
            ? Math.Round((decimal)totalTaken / totalScheduled * 100, 2)
            : 0m;

        return new CaregiverComplianceOverviewResponse
        {
            PatientId                  = patient.PatientId,
            PatientFullName            = patient.User.FullName,
            Days                       = windowDays,
            PeriodStartDate            = startDate,
            PeriodEndDate              = endDate,
            OverallCompliancePercentage = overallPct,
            ComplianceStreakDays       = patient.ComplianceStreakDays,
            MissedDoseCount            = totalMissed,
            MedicationRows             = rows,
        };
    }

    private async Task<List<CaregiverUpcomingDoseResponse>> BuildUpcomingDosesAsync(
        Guid patientId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var lookahead = today.AddDays(7);

        var activeMedications = await _db.Medications
            .Include(m => m.DoseSchedules)
            .AsNoTracking()
            .Where(m =>
                m.PatientId == patientId &&
                m.IsActive  &&
                m.StartDate <= lookahead &&
                (m.EndDate == null || m.EndDate >= today))
            .ToListAsync(cancellationToken);

        // Get already-logged intakes for lookahead window
        var existingLogs = await _db.IntakeLogs
            .AsNoTracking()
            .Where(l =>
                l.PatientId     == patientId &&
                l.ScheduledDate >= today     &&
                l.ScheduledDate <= lookahead)
            .ToListAsync(cancellationToken);

        var logLookup = existingLogs
            .GroupBy(l => new { l.DoseScheduleId, l.ScheduledDate })
            .ToDictionary(g => g.Key, g => g.First().Status);

        var upcoming = new List<CaregiverUpcomingDoseResponse>();

        foreach (var med in activeMedications)
        {
            var medStart = med.StartDate > today ? med.StartDate : today;
            var medEnd   = med.EndDate.HasValue && med.EndDate.Value < lookahead
                           ? med.EndDate.Value : lookahead;

            foreach (var schedule in med.DoseSchedules.Where(s => s.IsActive))
            {
                for (var d = medStart; d <= medEnd && upcoming.Count < UpcomingDoseCount; d = d.AddDays(1))
                {
                    if (schedule.DayOfWeek is not null && schedule.DayOfWeek != (int)d.DayOfWeek)
                        continue;

                    var key = new { DoseScheduleId = schedule.DoseScheduleId, ScheduledDate = d };
                    logLookup.TryGetValue(key, out var status);

                    upcoming.Add(new CaregiverUpcomingDoseResponse
                    {
                        DoseScheduleId = schedule.DoseScheduleId,
                        MedicationId   = med.MedicationId,
                        MedicationName = med.DrugName,
                        DosageStrength = med.DosageStrength,
                        ScheduledDate  = d,
                        ScheduledTime  = schedule.ScheduledTime,
                        IntakeStatus   = status,
                    });
                }

                if (upcoming.Count >= UpcomingDoseCount) break;
            }

            if (upcoming.Count >= UpcomingDoseCount) break;
        }

        return upcoming
            .OrderBy(u => u.ScheduledDate)
            .ThenBy(u => u.ScheduledTime)
            .Take(UpcomingDoseCount)
            .ToList();
    }

    private async Task<CaregiverServiceResult<CaregiverPatientLinkResponse>?> SaveOrFailAsync(
        CancellationToken cancellationToken,
        string operation,
        Guid linkId,
        Guid actorId)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return null; // success — caller checks null and proceeds
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Failed to {Operation} for LinkId {LinkId}, ActorId {ActorId}.",
                operation, linkId, actorId);
            return CaregiverServiceResult<CaregiverPatientLinkResponse>.Failure(
                CaregiverErrorCodes.SaveFailed,
                "A database error occurred. Please try again.",
                StatusCodes.Status500InternalServerError);
        }
    }

    private static CaregiverPatientLinkResponse MapLinkResponse(CaregiverPatientLink link) =>
        new()
        {
            LinkId                = link.CaregiverPatientLinkId,
            CaregiverId           = link.CaregiverId,
            PatientId             = link.PatientId,
            PatientFullName       = link.Patient?.User?.FullName ?? string.Empty,
            CaregiverFullName     = link.Caregiver?.User?.FullName ?? string.Empty,
            PermissionLevel       = "ReadOnly", // FR-16: caregivers are always read-only
            Status                = link.Status,
            RelationshipToPatient = null,
            RequestedByUserId     = link.PatientId.ToString(),
            RequestedAt           = link.CreatedAt,
            ApprovedAt            = link.ApprovedDate,
            RejectedAt            = null,
        };
}
