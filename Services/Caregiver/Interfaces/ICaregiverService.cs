using MediAlert.DTOs.Caregiver;

namespace MediAlert.Services.Caregiver.Interfaces;

/// <summary>
/// Module 6 — Caregiver Portal service contract.
///
/// Business rules enforced (FR-14, FR-15, FR-16):
///   FR-14: Patient initiates link by caregiver email. Duplicate blocked.
///          Only Caregiver-role users may be linked.
///   FR-15: Caregiver approves/rejects. Only the caregiver on the link
///          may action it. Data must not be visible before approval.
///   FR-16: Caregiver reads patient compliance data (read-only).
///          Access is strictly gated on Status == Approved.
///          Caregiver can never write intake logs.
/// </summary>
public interface ICaregiverService
{
    // ── Link lifecycle (FR-14, FR-15) ────────────────────────────────────────

    /// <summary>Patient sends a link request to a caregiver by email.</summary>
    Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> SendLinkRequestAsync(
        Guid patientId,
        CreateCaregiverPatientLinkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Caregiver approves a pending link request.</summary>
    Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> ApproveLinkRequestAsync(
        Guid linkId,
        Guid caregiverId,
        CancellationToken cancellationToken = default);

    /// <summary>Caregiver rejects a pending link request.</summary>
    Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> RejectLinkRequestAsync(
        Guid linkId,
        Guid caregiverId,
        string? rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an approved link.
    /// Either the patient or the caregiver may revoke.
    /// </summary>
    Task<CaregiverServiceResult<CaregiverPatientLinkResponse>> RevokeLinkAsync(
        Guid linkId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all link requests visible to a caregiver (pending + active).</summary>
    Task<CaregiverServiceResult<List<CaregiverPatientLinkResponse>>> GetCaregiverLinksAsync(
        Guid caregiverId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all links a patient has created.</summary>
    Task<CaregiverServiceResult<List<CaregiverPatientLinkResponse>>> GetPatientLinksAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    // ── Monitoring dashboard (FR-16) ─────────────────────────────────────────

    /// <summary>
    /// Returns the full monitoring dashboard for one of the caregiver's
    /// approved patients: compliance %, missed-dose alerts, upcoming 5 doses.
    /// Access is rejected if link is not Approved.
    /// </summary>
    Task<CaregiverServiceResult<CaregiverMonitoringDashboardResponse>> GetPatientDashboardAsync(
        Guid caregiverId,
        Guid patientId,
        int windowDays,
        CancellationToken cancellationToken = default);
}
