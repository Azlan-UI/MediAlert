using System.Security.Claims;

namespace MediAlert.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return Guid.Empty;

        return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
    }
}
