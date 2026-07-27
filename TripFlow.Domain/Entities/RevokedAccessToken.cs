namespace TripFlow.Domain.Entities;

/// <summary>
/// Denylist de access tokens revogados antes do vencimento natural (logout, troca de senha,
/// deteccao de reuso de refresh token). Chave e o "jti" do JWT; ExpiresAt so existe pra permitir
/// limpar linhas de tokens que ja venceriam de qualquer jeito.
/// </summary>
public class RevokedAccessToken
{
    public required string Jti { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
}
