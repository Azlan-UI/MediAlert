namespace MediAlert.Constants;

/// <summary>
/// All valid invoice status values.
/// Maps to Invoice.Status column.
/// </summary>
public static class InvoiceStatuses
{
    public const string Pending = "Pending";
    public const string Paid    = "Paid";
    public const string Failed  = "Failed";
    public const string Open    = "Open";
    public const string Void    = "Void";

    public static readonly IReadOnlyList<string> All = [Pending, Paid, Failed, Open, Void];

    public static bool IsValid(string status) =>
        All.Contains(status, StringComparer.OrdinalIgnoreCase);
}
