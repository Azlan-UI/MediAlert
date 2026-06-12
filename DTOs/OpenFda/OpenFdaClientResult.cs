using System.Net;

namespace MediAlert.DTOs.OpenFda;

public sealed class OpenFdaClientResult<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public HttpStatusCode? StatusCode { get; init; }
    public TimeSpan? RetryAfter { get; init; }

    public static OpenFdaClientResult<T> Success(T data, HttpStatusCode? statusCode = null) =>
        new()
        {
            Succeeded = true,
            Data = data,
            StatusCode = statusCode,
        };

    public static OpenFdaClientResult<T> Failure(
        string errorCode,
        string errorMessage,
        HttpStatusCode? statusCode = null,
        TimeSpan? retryAfter = null) =>
        new()
        {
            Succeeded = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
            RetryAfter = retryAfter,
        };
}
