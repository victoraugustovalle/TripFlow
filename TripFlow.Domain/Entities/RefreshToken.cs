namespace TripFlow.Domain.Entities;

/// <summary>
/// So o hash do token e persistido (o valor em claro so existe no cookie do cliente).
/// FamilyId agrupa toda a cadeia de rotacao de uma sessao: reapresentar um token
/// ja substituido (RevokedAt preenchido) indica roubo, e derruba a familia inteira.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }

    public required string TokenHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt is null && !IsExpired;
}
