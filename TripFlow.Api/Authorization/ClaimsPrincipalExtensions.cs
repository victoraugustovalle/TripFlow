using System.Security.Claims;

namespace TripFlow.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Token sem claim de usuario.");
        return Guid.Parse(value);
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new InvalidOperationException("Token sem claim de e-mail.");
    }
}
