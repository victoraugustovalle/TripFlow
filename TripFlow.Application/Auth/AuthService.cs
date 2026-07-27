using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Common;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Auth;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IEmailSender _emailSender;
    private readonly AuthPolicyOptions _policy;
    private readonly TwoFactorOptions _twoFactorOptions;
    private readonly ILogger<AuthService> _logger;

    private readonly Lazy<string> _dummyPasswordHash;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IGoogleAuthService googleAuthService,
        IEmailSender emailSender,
        IOptions<AuthPolicyOptions> policy,
        IOptions<TwoFactorOptions> twoFactorOptions,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _googleAuthService = googleAuthService;
        _emailSender = emailSender;
        _policy = policy.Value;
        _twoFactorOptions = twoFactorOptions.Value;
        _logger = logger;

        // Usado no login quando o e-mail nao existe, so pra gastar o mesmo tempo de CPU
        // que gastaria verificando uma senha real - evita que o tempo de resposta denuncie
        // se o e-mail esta cadastrado ou nao.
        _dummyPasswordHash = new Lazy<string>(() => _passwordHasher.Hash(Guid.NewGuid().ToString()));
    }

    public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var alreadyExists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (alreadyExists)
            return ServiceResult<UserDto>.Failure(ServiceErrorType.Conflict, "Ja existe uma conta com esse e-mail.");

        var code = TokenHasher.GenerateNumericCode();

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            EmailConfirmed = false,
            EmailConfirmationCodeHash = TokenHasher.Hash(code),
            EmailConfirmationCodeExpiresAt = DateTime.UtcNow.AddMinutes(_policy.EmailConfirmationCodeExpiryMinutes)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await TrySendEmailAsync(user.Email, "Confirme seu e-mail",
            $"<p>Seu codigo de confirmacao e <strong>{code}</strong>. Ele expira em {_policy.EmailConfirmationCodeExpiryMinutes} minutos.</p>", ct);

        return ServiceResult<UserDto>.Success(ToDto(user));
    }

    public async Task<ServiceResult<bool>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var user = await FindByEmailAsync(request.Email, ct);
        if (user is null || user.EmailConfirmationCodeHash is null || user.EmailConfirmationCodeExpiresAt is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Codigo invalido.");

        if (user.EmailConfirmationCodeExpiresAt < DateTime.UtcNow || !TokenHasher.Matches(request.Code, user.EmailConfirmationCodeHash))
            return ServiceResult<bool>.Failure(ServiceErrorType.Validation, "Codigo invalido ou expirado.");

        user.EmailConfirmed = true;
        user.EmailConfirmationCodeHash = null;
        user.EmailConfirmationCodeExpiresAt = null;
        await _db.SaveChangesAsync(ct);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<LoginResult>> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default)
    {
        var user = await FindByEmailAsync(request.Email, ct);

        if (user is null)
        {
            _passwordHasher.Verify(request.Password, _dummyPasswordHash.Value);
            return ServiceResult<LoginResult>.Failure(ServiceErrorType.Unauthorized, "E-mail ou senha invalidos.");
        }

        if (user.IsLockedOut)
            return ServiceResult<LoginResult>.Failure(ServiceErrorType.LockedOut, "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");

        if (user.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await RegisterFailedLoginAsync(user, ct);
            return ServiceResult<LoginResult>.Failure(ServiceErrorType.Unauthorized, "E-mail ou senha invalidos.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        if (user.TwoFactorEnabled)
        {
            var challenge = TokenHasher.GenerateUrlSafeToken();
            user.TwoFactorChallengeTokenHash = TokenHasher.Hash(challenge);
            user.TwoFactorChallengeExpiresAt = DateTime.UtcNow.AddMinutes(_twoFactorOptions.ChallengeExpiryMinutes);
            await _db.SaveChangesAsync(ct);

            return ServiceResult<LoginResult>.Success(new LoginResult(true, challenge, null));
        }

        var result = await IssueTokensAsync(user, Guid.NewGuid(), ip, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<LoginResult>.Success(new LoginResult(false, null, result));
    }

    /// <summary>Emite o par de tokens pra um usuario que ja provou o segundo fator (chamado pelo TwoFactorService apos validar o codigo TOTP ou o codigo de recuperacao).</summary>
    public async Task<AuthResult> IssueTokensForVerifiedUserAsync(Guid userId, string? ip, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("Usuario nao encontrado ao emitir token pos-verificacao.");

        var result = await IssueTokensAsync(user, Guid.NewGuid(), ip, ct);
        await _db.SaveChangesAsync(ct);

        return result;
    }

    public async Task<ServiceResult<LoginResult>> GoogleLoginAsync(GoogleLoginRequest request, string? ip, CancellationToken ct = default)
    {
        var googleUser = await _googleAuthService.ValidateIdTokenAsync(request.IdToken, ct);
        if (googleUser is null || !googleUser.EmailVerified)
            return ServiceResult<LoginResult>.Failure(ServiceErrorType.Unauthorized, "Token do Google invalido.");

        var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
        var user = await FindByEmailAsync(normalizedEmail, ct);

        if (user is null)
        {
            user = new User
            {
                Name = googleUser.Name,
                Email = normalizedEmail,
                GoogleId = googleUser.GoogleId,
                EmailConfirmed = true
            };
            _db.Users.Add(user);
        }
        else if (user.GoogleId is null)
        {
            user.GoogleId = googleUser.GoogleId;
        }

        // Login com Google nao pula o segundo fator - se a conta tem 2FA ligado, precisa do
        // codigo do app autenticador mesmo entrando pelo Google (senao o 2FA vira so decorativo
        // pra quem tambem linkou login social).
        if (user.TwoFactorEnabled)
        {
            var challenge = TokenHasher.GenerateUrlSafeToken();
            user.TwoFactorChallengeTokenHash = TokenHasher.Hash(challenge);
            user.TwoFactorChallengeExpiresAt = DateTime.UtcNow.AddMinutes(_twoFactorOptions.ChallengeExpiryMinutes);
            await _db.SaveChangesAsync(ct);

            return ServiceResult<LoginResult>.Success(new LoginResult(true, challenge, null));
        }

        var result = await IssueTokensAsync(user, Guid.NewGuid(), ip, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<LoginResult>.Success(new LoginResult(false, null, result));
    }

    public async Task<ServiceResult<AuthResult>> RefreshAsync(string rawRefreshToken, string? ip, CancellationToken ct = default)
    {
        var hash = _tokenService.HashRefreshToken(rawRefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Sessao invalida.");

        if (stored.RevokedAt is not null)
        {
            _logger.LogWarning("Reuso de refresh token detectado para o usuario {UserId} - revogando a familia {FamilyId}", stored.UserId, stored.FamilyId);
            await RevokeFamilyAsync(stored.FamilyId, ct);
            await _db.SaveChangesAsync(ct);
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Sessao invalida, faca login novamente.");
        }

        if (stored.IsExpired)
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Sessao expirada, faca login novamente.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
        if (user is null)
            return ServiceResult<AuthResult>.Failure(ServiceErrorType.Unauthorized, "Sessao invalida.");

        var result = await IssueTokensAsync(user, stored.FamilyId, ip, ct, revokeAndReplace: stored);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<AuthResult>.Success(result);
    }

    public async Task LogoutAsync(string? rawRefreshToken, string? accessTokenJti, DateTime? accessTokenExpiresAt, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(rawRefreshToken))
        {
            var hash = _tokenService.HashRefreshToken(rawRefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is not null && stored.RevokedAt is null)
                stored.RevokedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(accessTokenJti) && accessTokenExpiresAt is not null)
        {
            _db.RevokedAccessTokens.Add(new RevokedAccessToken { Jti = accessTokenJti, ExpiresAt = accessTokenExpiresAt.Value });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ServiceResult<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await FindByEmailAsync(request.Email, ct);
        if (user is not null)
        {
            var token = TokenHasher.GenerateUrlSafeToken();
            user.PasswordResetTokenHash = TokenHasher.Hash(token);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(_policy.PasswordResetExpiryMinutes);
            await _db.SaveChangesAsync(ct);

            await TrySendEmailAsync(user.Email, "Redefinicao de senha",
                $"<p>Use este codigo pra redefinir sua senha: <strong>{token}</strong>. Expira em {_policy.PasswordResetExpiryMinutes} minutos.</p>", ct);
        }

        // Resposta identica exista ou nao o e-mail - nao da pra usar esse endpoint pra descobrir contas cadastradas.
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await FindByEmailAsync(request.Email, ct);
        if (user is null || user.PasswordResetTokenHash is null || user.PasswordResetTokenExpiresAt is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.Validation, "Token invalido ou expirado.");

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow || !TokenHasher.Matches(request.Token, user.PasswordResetTokenHash))
            return ServiceResult<bool>.Failure(ServiceErrorType.Validation, "Token invalido ou expirado.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        // Troca de senha derruba todas as sessoes ativas - se alguem comprometeu a senha
        // antiga, os refresh tokens emitidos com ela nao podem continuar validos.
        var activeFamilies = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .Select(t => t.FamilyId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var familyId in activeFamilies)
            await RevokeFamilyAsync(familyId, ct);

        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    private async Task RegisterFailedLoginAsync(User user, CancellationToken ct)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= _policy.MaxFailedLoginAttempts)
        {
            user.LockedUntil = DateTime.UtcNow.AddMinutes(_policy.LockoutMinutes);
            _logger.LogWarning("Usuario {UserId} bloqueado por {Minutes} minutos apos {Attempts} tentativas de login falhas", user.Id, _policy.LockoutMinutes, user.FailedLoginAttempts);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, Guid familyId, string? ip, CancellationToken ct, RefreshToken? revokeAndReplace = null)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshTokenValue();
        var refreshTokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var refreshExpiresAt = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime);

        if (revokeAndReplace is not null)
        {
            revokeAndReplace.RevokedAt = DateTime.UtcNow;
            revokeAndReplace.ReplacedByTokenHash = refreshTokenHash;
        }

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            CreatedByIp = ip
        });

        return new AuthResult(accessToken.Token, accessToken.ExpiresAt, rawRefreshToken, refreshExpiresAt, ToDto(user));
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;
    }

    private Task<User?> FindByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    private async Task TrySendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            await _emailSender.SendAsync(to, subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail para {Email} - fluxo segue normalmente, so o envio falhou", to);
        }
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.GlobalRole, user.EmailConfirmed, user.TwoFactorEnabled);
}
