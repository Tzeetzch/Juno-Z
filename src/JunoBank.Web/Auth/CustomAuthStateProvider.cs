using System.Security.Claims;
using JunoBank.Web.Data.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace JunoBank.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<UserSession>("UserSession");

            if (result.Success && result.Value != null)
            {
                var session = result.Value;
                var claims = CreateClaims(session);
                var identity = new ClaimsIdentity(claims, "CustomAuth");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch
        {
            // Session storage not available (e.g., prerendering)
        }

        return new AuthenticationState(_anonymous);
    }

    public async Task LoginAsync(UserSession session)
    {
        await _sessionStorage.SetAsync("UserSession", session);

        var claims = CreateClaims(session);
        var identity = new ClaimsIdentity(claims, "CustomAuth");
        var principal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task LogoutAsync()
    {
        await _sessionStorage.DeleteAsync("UserSession");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    public async Task<UserSession?> GetCurrentUserAsync()
    {
        try
        {
            var result = await _sessionStorage.GetAsync<UserSession>("UserSession");
            return result.Success ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<Claim> CreateClaims(UserSession session)
    {
        return new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.UserName),
            new(ClaimTypes.Role, session.Role.ToString())
        };
    }
}
