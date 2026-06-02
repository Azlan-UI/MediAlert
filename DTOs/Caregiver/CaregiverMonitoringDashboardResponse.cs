namespace MediAlert.DTOs.Caregiver;

public sealed class CaregiverMonitoringDashboardResponse
{
  public Guid PatientId { get; set; }

  public string PatientFullName { get; set; } = string.Empty;

  public string? PatientPhoneNumber { get; set; }

  public string? PatientEmail { get; set; }

  public CaregiverComplianceOverviewResponse ComplianceOverview { get; set; } = new();

  public List<CaregiverMissedDoseAlertResponse> MissedDoseAlerts { get; set; } = [];

  public List<CaregiverUpcomingDoseResponse> UpcomingDoses { get; set; } = [];
}
