using System.ComponentModel.DataAnnotations;
using MediAlert.DTOs.Common;

namespace MediAlert.DTOs.Reports;

public sealed class ComplianceReportSearchRequest : PagedSortRequest
{
  [Required(ErrorMessage = "Patient ID is required.")]
  public Guid PatientId { get; set; }

  public DateOnly? PeriodStartFrom { get; set; }

  public DateOnly? PeriodStartTo { get; set; }

  public DateOnly? PeriodEndFrom { get; set; }

  public DateOnly? PeriodEndTo { get; set; }

  public bool? HasPdfExport { get; set; }
}
