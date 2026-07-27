using System.Security.Claims;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.Abstractions;

public record GeneratedAccessToken(string Token, string Jti, DateTime ExpiresAt);

public interface ITokenService
{
    GeneratedAccessToken GenerateAccessToken(User user, IEnumerable<Claim>? extraClaims = null);

    /// <summary>Valor aleatorio de alta entropia enviado ao cliente - so o hash dele e persistido.</summary>
    string GenerateRefreshTokenValue();

    string HashRefreshToken(string rawValue);

    TimeSpan RefreshTokenLifetime { get; }
}
