namespace TripFlow.Application.Common;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string PrivateKeyPem { get; set; }
    public required string PublicKeyPem { get; set; }
    public string Issuer { get; set; } = "TripFlow";
    public string Audience { get; set; } = "TripFlow.Api";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
