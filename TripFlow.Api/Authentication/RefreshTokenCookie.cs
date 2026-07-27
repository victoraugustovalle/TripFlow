namespace TripFlow.Api.Authentication;

/// <summary>Centraliza nome/path do cookie do refresh token - usado tanto no login normal
/// quanto no /2fa/verify, e precisa bater exatamente com o que /api/auth/refresh le.</summary>
public static class RefreshTokenCookie
{
    public const string Name = "refreshToken";

    public static void Append(HttpResponse response, string rawToken, DateTime expiresAt)
    {
        response.Cookies.Append(Name, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = expiresAt
        });
    }
}
