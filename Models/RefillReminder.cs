using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediAlert.Models;

public class RefillReminder
{
    [Key]
    public Guid ReminderId { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid MedicationId { get; set; }

    [Required]
    public DateTime ReminderDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Acknowledged, Dismissed

    [Required]
    public DateTime CreatedAt { get; set; }

    public virtual Patient Patient { get; set; } = null!;
    public virtual Medication Medication { get; set; } = null!;
}
