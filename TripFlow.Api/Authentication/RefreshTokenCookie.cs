namespace TripFlow.Api.Authentication;

/// <summary>Centraliza nome/path do cookie do refresh token - usado tanto no login normal
/// quanto no /2fa/verify, e precisa bater exatamente com o que /api/auth/refresh le.</summary>
public static class RefreshTokenCookie
{
    public const string Name = "refreshToken";

    /// <summary>
    /// Frontend e API vivem em dominios diferentes (ex: Vercel + Fly.io) - isso e cross-site,
    /// entao o cookie precisa de SameSite=None (com Secure) pra sobreviver a um refresh silencioso
    /// ou a um F5. O CSRF que SameSite=Strict mitigava fica coberto pela allow-list de CORS: so
    /// origem cadastrada consegue mandar request com credentials pra essa API.
    /// </summary>
    private static CookieOptions BaseOptions => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/api/auth"
    };

    public static void Append(HttpResponse response, string rawToken, DateTime expiresAt)
    {
        var options = BaseOptions;
        options.Expires = expiresAt;
        response.Cookies.Append(Name, rawToken, options);
    }

    /// <summary>Cookies.Delete(key) sem options usa Path="/" por padrao, que nao bate com o Path
    /// deste cookie - o navegador ignora o delete e o cookie velho continua la. Precisa repetir
    /// as mesmas options do Append pra realmente casar e remover.</summary>
    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name, BaseOptions);
}
