namespace MediAlert.DTOs.Appointments;

public class RefillReminderDto
{
    public Guid ReminderId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public DateTime ReminderDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
