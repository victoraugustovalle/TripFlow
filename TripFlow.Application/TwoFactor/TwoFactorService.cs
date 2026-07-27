using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Auth;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Common;
using TripFlow.Application.TwoFactor.DTOs;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.TwoFactor;

public class TwoFactorService
{
    private readonly IAppDbContext _db;
    private readonly ITotpService _totpService;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthService _authService;
    private readonly TwoFactorOptions _options;

    public TwoFactorService(
        IAppDbContext db,
        ITotpService totpService,
        IQrCodeGenerator qrCodeGenerator,
        IPasswordHasher passwordHasher,
        AuthService authService,
        IOptions<TwoFactorOptions> options)
    {
        _db = db;
        _totpService = totpService;
        _qrCodeGenerator = qrCodeGenerator;
        _passwordHasher = passwordHasher;
        _authService = authService;
        _options = options.Value;
    }

    /// <summary>Gera um segredo novo e guarda "pendente" (2FA continua desligado ate confirmar
    /// via EnableAsync) - chamar de novo antes de confirmar so substitui o pendente anterior.</summary>
    public async Task<ServiceResult<TwoFactorSetupResult>> SetupAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ServiceResult<TwoFactorSetupResult>.Failure(ServiceErrorType.NotFound, "Usuario nao encontrado.");

        var secret = _totpService.GenerateSecret();
        user.TwoFactorSecret = secret;
        user.TwoFactorEnabled = false;
        await _db.SaveChangesAsync(ct);

        var otpAuthUri = _totpService.BuildProvisioningUri(secret, user.Email, _options.Issuer);
        var qrCodePng = _qrCodeGenerator.GeneratePng(otpAuthUri);

        return ServiceResult<TwoFactorSetupResult>.Success(new TwoFactorSetupResult(secret, otpAuthUri, Convert.ToBase64String(qrCodePng)));
    }

    /// <summary>Confirma que o usuario configurou o app autenticador certo (validando um codigo
    /// gerado por ele) e liga o 2FA de vez. Devolve os codigos de recuperacao em claro - so
    /// dessa vez, depois so o hash fica salvo.</summary>
    public async Task<ServiceResult<IReadOnlyList<string>>> EnableAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ServiceResult<IReadOnlyList<string>>.Failure(ServiceErrorType.NotFound, "Usuario nao encontrado.");

        if (user.TwoFactorSecret is null)
            return ServiceResult<IReadOnlyList<string>>.Failure(ServiceErrorType.Validation, "Chame /2fa/setup antes de confirmar.");

        if (!_totpService.ValidateCode(user.TwoFactorSecret, code))
            return ServiceResult<IReadOnlyList<string>>.Failure(ServiceErrorType.Validation, "Codigo invalido.");

        user.TwoFactorEnabled = true;

        var oldCodes = await _db.TwoFactorRecoveryCodes.Where(c => c.UserId == userId).ToListAsync(ct);
        _db.TwoFactorRecoveryCodes.RemoveRange(oldCodes);

        var rawCodes = new List<string>(_options.RecoveryCodeCount);
        for (var i = 0; i < _options.RecoveryCodeCount; i++)
        {
            var raw = TokenHasher.GenerateRecoveryCode();
            rawCodes.Add(raw);
            _db.TwoFactorRecoveryCodes.Add(new TwoFactorRecoveryCode { UserId = userId, CodeHash = TokenHasher.Hash(raw) });
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult<IReadOnlyList<string>>.Success(rawCodes);
    }

    /// <summary>Desliga o 2FA - exige senha e um codigo TOTP valido, pra nao dar pra desligar so com o access token roubado.</summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid userId, string password, string code, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Usuario nao encontrado.");

        if (user.PasswordHash is null || !_passwordHasher.Verify(password, user.PasswordHash))
            return ServiceResult<bool>.Failure(ServiceErrorType.Unauthorized, "Senha invalida.");

        if (!user.TwoFactorEnabled || user.TwoFactorSecret is null || !_totpService.ValidateCode(user.TwoFactorSecret, code))
            return ServiceResult<bool>.Failure(ServiceErrorType.Validation, "Codigo invalido.");

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;

        var codes = await _db.TwoFactorRecoveryCodes.Where(c => c.UserId == userId).ToListAsync(ct);
        _db.TwoFactorRecoveryCodes.RemoveRange(codes);

        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    /// <summary>Segundo passo do login quando a conta tem 2FA: confere o desafio emitido no
    /// login e o codigo (TOTP ou de recuperacao), e so entao emite o token de verdade.</summary>
    public async Task<ServiceResult<AuthResult>> VerifyChallengeAsync(VerifyTwoFactorRequest request, string? ip, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || user.TwoFactorChallengeTokenHash is null || user.TwoFactorChallengeExpiresAt is null)
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Desafio invalido.");

        if (user.TwoFactorChallengeExpiresAt < DateTime.UtcNow || !TokenHasher.Matches(request.ChallengeToken, user.TwoFactorChallengeTokenHash))
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Desafio invalido ou expirado - faca login de novo.");

        var verified = false;

        if (!string.IsNullOrEmpty(request.Code) && user.TwoFactorSecret is not null)
        {
            verified = _totpService.ValidateCode(user.TwoFactorSecret, request.Code);
        }
        else if (!string.IsNullOrEmpty(request.RecoveryCode))
        {
            verified = await TryConsumeRecoveryCodeAsync(user.Id, request.RecoveryCode, ct);
        }

        if (!verified)
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Codigo invalido.");

        user.TwoFactorChallengeTokenHash = null;
        user.TwoFactorChallengeExpiresAt = null;

        var result = await _authService.IssueTokensForVerifiedUserAsync(user.Id, ip, ct);
        return ServiceResult<AuthResult>.Success(result);
    }

    private async Task<bool> TryConsumeRecoveryCodeAsync(Guid userId, string rawCode, CancellationToken ct)
    {
        var candidates = await _db.TwoFactorRecoveryCodes
            .Where(c => c.UserId == userId && c.UsedAt == null)
            .ToListAsync(ct);

        var match = candidates.FirstOrDefault(c => TokenHasher.Matches(rawCode, c.CodeHash));
        if (match is null)
            return false;

        match.UsedAt = DateTime.UtcNow;
        return true;
    }
}
