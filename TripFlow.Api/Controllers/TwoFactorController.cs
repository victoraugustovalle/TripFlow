using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TripFlow.Api.Authentication;
using TripFlow.Api.Authorization;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.TwoFactor;
using TripFlow.Application.TwoFactor.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/auth/2fa")]
[EnableRateLimiting("auth")]
public class TwoFactorController : ApiControllerBase
{
    private readonly TwoFactorService _twoFactorService;

    public TwoFactorController(TwoFactorService twoFactorService)
    {
        _twoFactorService = twoFactorService;
    }

    /// <summary>Gera um segredo TOTP novo (2FA continua desligado ate confirmar em /enable) e devolve o QR code (PNG em base64) e o segredo em texto, pra digitar manualmente se preferir.</summary>
    [HttpPost("setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupResult>> Setup(CancellationToken ct)
    {
        var result = await _twoFactorService.SetupAsync(User.GetUserId(), ct);
        return FromResult(result);
    }

    /// <summary>Confirma o setup com um codigo gerado pelo app autenticador e liga o 2FA de vez. Devolve os codigos de recuperacao - guarde agora, eles nao aparecem de novo.</summary>
    [HttpPost("enable")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<string>>> Enable([FromBody] EnableTwoFactorRequest request, CancellationToken ct)
    {
        var result = await _twoFactorService.EnableAsync(User.GetUserId(), request.Code, ct);
        return FromResult(result);
    }

    /// <summary>Desliga o 2FA - exige a senha e um codigo TOTP valido (nao da pra desligar so com o access token, caso ele vaze).</summary>
    [HttpPost("disable")]
    [Authorize]
    public async Task<IActionResult> Disable([FromBody] DisableTwoFactorRequest request, CancellationToken ct)
    {
        var result = await _twoFactorService.DisableAsync(User.GetUserId(), request.Password, request.Code, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    /// <summary>Segundo passo do login quando a conta tem 2FA: manda o token de desafio recebido no /login junto do codigo do app autenticador (ou um codigo de recuperacao, se perdeu o celular).</summary>
    [HttpPost("verify")]
    [EnableRateLimiting("auth-login")]
    public async Task<ActionResult<AccessTokenResponse>> Verify([FromBody] VerifyTwoFactorRequest request, CancellationToken ct)
    {
        var result = await _twoFactorService.VerifyChallengeAsync(request, GetClientIp(), ct);
        if (!result.Succeeded)
            return FromResult(result).Result!;

        RefreshTokenCookie.Append(Response, result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new AccessTokenResponse(result.Value.AccessToken, result.Value.AccessTokenExpiresAt, result.Value.User));
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
