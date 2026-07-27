using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TripFlow.Api.Authorization;
using TripFlow.Application.Auth;
using TripFlow.Application.Auth.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ApiControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Cria uma conta nova. A senha precisa ter maiuscula, minuscula, numero e caractere especial. O e-mail comeca nao confirmado - um codigo de 6 digitos e enviado (ou logado, se o SMTP nao estiver configurado).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return FromResult(result);
    }

    /// <summary>Confirma o e-mail com o codigo de 6 digitos recebido no registro. O codigo expira em 15 minutos.</summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var result = await _authService.ConfirmEmailAsync(request, ct);
        return FromResult(result).Result ?? Ok();
    }

    /// <summary>Login com e-mail e senha. Devolve o access token no corpo e o refresh token num cookie HttpOnly. Bloqueia a conta por um tempo apos varias tentativas erradas.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<AccessTokenResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, GetClientIp(), ct);
        if (!result.Succeeded)
            return FromResult(result).Result!;

        SetRefreshTokenCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(ToAccessTokenResponse(result.Value));
    }

    /// <summary>Login (ou cadastro automatico) usando o id_token do Google Sign-In. Cria a conta na primeira vez e ja marca o e-mail como confirmado.</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AccessTokenResponse>> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        var result = await _authService.GoogleLoginAsync(request, GetClientIp(), ct);
        if (!result.Succeeded)
            return FromResult(result).Result!;

        SetRefreshTokenCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(ToAccessTokenResponse(result.Value));
    }

    /// <summary>Troca o refresh token (cookie) por um par novo de access+refresh token. O token antigo e invalidado; se ja tiver sido usado antes, a sessao inteira e revogada (sinal de roubo de token).</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<AccessTokenResponse>> Refresh(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(new { message = "Sessao invalida." });

        var result = await _authService.RefreshAsync(rawToken, GetClientIp(), ct);
        if (!result.Succeeded)
        {
            Response.Cookies.Delete(RefreshCookieName);
            return FromResult(result).Result!;
        }

        SetRefreshTokenCookie(result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(ToAccessTokenResponse(result.Value));
    }

    /// <summary>Encerra a sessao atual: revoga o refresh token e coloca o access token atual numa denylist (por jti), impedindo seu uso antes do vencimento natural.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rawToken = Request.Cookies[RefreshCookieName];
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = User.FindFirst("exp")?.Value;
        DateTime? expiresAt = long.TryParse(expClaim, out var expSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime
            : null;

        await _authService.LogoutAsync(rawToken, jti, expiresAt, ct);
        Response.Cookies.Delete(RefreshCookieName);
        return NoContent();
    }

    /// <summary>Inicia a redefinicao de senha. A resposta e sempre a mesma exista ou nao o e-mail cadastrado, pra nao vazar quais contas existem. Se existir, um token de reset e enviado (ou logado).</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request, ct);
        return Ok(new { message = "Se esse e-mail estiver cadastrado, voce vai receber instrucoes de redefinicao." });
    }

    /// <summary>Define uma senha nova usando o token recebido no forgot-password. Derruba todas as sessoes ativas do usuario (refresh tokens de todas as familias sao revogados).</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(request, ct);
        return FromResult(result).Result ?? Ok();
    }

    private void SetRefreshTokenCookie(string rawToken, DateTime expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = expiresAt
        });
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static AccessTokenResponse ToAccessTokenResponse(AuthResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAt, result.User);
}

public record AccessTokenResponse(string AccessToken, DateTime AccessTokenExpiresAt, UserDto User);
