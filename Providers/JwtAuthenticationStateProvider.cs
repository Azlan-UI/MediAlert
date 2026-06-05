using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using MediAlert.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

namespace MediAlert.Providers;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtAuthenticationStateProvider(
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return new AuthenticationState(httpContext.User);
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
