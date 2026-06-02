using MediAlert.Constants;
using Microsoft.AspNetCore.Http;

namespace MediAlert.Services.Billing;

public sealed class BillingServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; } = StatusCodes.Status200OK;

    public static BillingServiceResult<T> Success(T data, int statusCode = StatusCodes.Status200OK) =>
        new() { Succeeded = true, Data = data, StatusCode = statusCode };

    public static BillingServiceResult<T> Failure(
        string errorCode, string errorMessage,
        int statusCode = StatusCodes.Status400BadRequest) =>
        new() { Succeeded = false, ErrorCode = errorCode, ErrorMessage = errorMessage, StatusCode = statusCode };
}
