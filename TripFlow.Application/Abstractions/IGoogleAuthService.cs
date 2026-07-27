namespace TripFlow.Application.Abstractions;

public record GoogleUserInfo(string GoogleId, string Email, string Name, bool EmailVerified);

public interface IGoogleAuthService
{
    /// <summary>Retorna null se o id_token for invalido, expirado ou nao emitido pro nosso client id.</summary>
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
