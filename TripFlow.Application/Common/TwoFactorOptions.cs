namespace TripFlow.Application.Common;

public class TwoFactorOptions
{
    public const string SectionName = "TwoFactor";

    /// <summary>Chave AES-256 em base64 (32 bytes) - gere com `openssl rand -base64 32`. Nunca commitada.</summary>
    public string EncryptionKeyBase64 { get; set; } = string.Empty;

    public string Issuer { get; set; } = "TripFlow";
    public int RecoveryCodeCount { get; set; } = 8;
    public int ChallengeExpiryMinutes { get; set; } = 5;
}
