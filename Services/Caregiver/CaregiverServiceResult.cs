using MediAlert.Constants;
using MediAlert.Data;
using MediAlert.DTOs.Caregiver;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ApplicationUser = MediAlert.Models.ApplicatioUser;

namespace MediAlert.Services.Caregiver;

// ═══════════════════════════════════════════════════════════════════════════
// ServiceResult — mirrors ComplianceServiceResult<T> exactly.
// ═══════════════════════════════════════════════════════════════════════════

public sealed class CaregiverServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; } = StatusCodes.Status200OK;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    public static CaregiverServiceResult<T> Success(T data, int statusCode = StatusCodes.Status200OK) =>
        new() { Succeeded = true, Data = data, StatusCode = statusCode };

    public static CaregiverServiceResult<T> Failure(
        string errorCode, string errorMessage,
        int statusCode = StatusCodes.Status400BadRequest) =>
        new() { Succeeded = false, ErrorCode = errorCode, ErrorMessage = errorMessage, StatusCode = statusCode };

    public static CaregiverServiceResult<T> ValidationFailure(
        Dictionary<string, string[]> errors) =>
        new()
        {
            Succeeded = false,
            ErrorCode = CaregiverErrorCodes.InvalidRequest,
            ErrorMessage = "Validation failed.",
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            ValidationErrors = errors,
        };
}
