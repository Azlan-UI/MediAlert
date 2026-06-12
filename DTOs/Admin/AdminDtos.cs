namespace MediAlert.DTOs.Admin;

public class AdminUserOverviewDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class AdminDoctorOverviewDto
{
    public Guid DoctorId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VerifyDoctorRequest
{
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}

public class AdminConsultationOverviewDto
{
    public Guid ConsultationId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class AdminSubscriptionOverviewDto
{
    public Guid SubscriptionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}
