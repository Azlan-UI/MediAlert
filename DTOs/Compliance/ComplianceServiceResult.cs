namespace MediAlert.DTOs.Compliance;

public sealed class ComplianceServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    public static ComplianceServiceResult<T> Success(T data, int statusCode = StatusCodes.Status200OK) =>
        new()
        {
            Succeeded = true,
            Data = data,
            StatusCode = statusCode,
        };

    public static ComplianceServiceResult<T> Failure(
        string errorCode,
        string errorMessage,
        int statusCode = StatusCodes.Status400BadRequest) =>
        new()
        {
            Succeeded = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
        };

    public static ComplianceServiceResult<T> ValidationFailure(
        Dictionary<string, string[]> errors,
        string errorMessage = "Validation failed.") =>
        new()
        {
            Succeeded = false,
            ErrorCode = MediAlert.Constants.ComplianceErrorCodes.InvalidRequest,
            ErrorMessage = errorMessage,
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            ValidationErrors = errors,
        };
}
