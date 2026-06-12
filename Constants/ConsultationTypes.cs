namespace MediAlert.Constants;

/// <summary>
/// All valid consultation type values.
/// Maps to Consultation.Type column.
/// </summary>
public static class ConsultationTypes
{
    public const string Video    = "Video";
    public const string InPerson = "InPerson";

    public static readonly IReadOnlyList<string> All = [Video, InPerson];

    public static bool IsValid(string type) =>
        All.Contains(type, StringComparer.OrdinalIgnoreCase);
}
