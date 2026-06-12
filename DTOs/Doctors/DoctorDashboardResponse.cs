using System.Collections.Generic;

namespace MediAlert.DTOs.Doctors;

public class DoctorDashboardResponse
{
    public int TodaysAppointmentsCount { get; set; }
    public int PendingConsultationsCount { get; set; }
    public int TotalPatientsCount { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public List<ConsultationSummaryResponse> UpcomingAppointments { get; set; } = new();
}
