using MediAlert.DTOs.Auth;
using MediAlert.Models;
using Microsoft.AspNetCore.Identity;

namespace MediAlert.Services.Auth;

// ═══════════════════════════════════════════════════════════════════════════
// WHY SPLIT INTERFACE AND IMPLEMENTATION?
//
// 1. TESTABILITY: Unit tests inject a mock IAuthService without touching
//    the real database.
// 2. DEPENDENCY INVERSION: Controllers depend on the abstraction (interface),
//    not the concrete class. You could swap the entire auth implementation
//    without changing the controller.
// 3. TEAM SAFETY: Teammates can call your service methods by the interface
//    contract without needing to read your implementation.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Contract for all authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    Task<AuthServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<AuthServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<AuthServiceResult<bool>> SuspendUserAsync(string targetUserId, string adminUserId);
    Task<AuthServiceResult<bool>> UnsuspendUserAsync(string targetUserId, string adminUserId);
}
