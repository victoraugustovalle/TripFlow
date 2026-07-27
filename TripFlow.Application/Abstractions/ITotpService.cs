namespace TripFlow.Application.Abstractions;

public interface ITotpService
{
    /// <summary>Gera um novo segredo TOTP (base32), pra guardar associado ao usuario.</summary>
    string GenerateSecret();

    /// <summary>Confere o codigo de 6 digitos do app autenticador contra o segredo, com tolerancia de 1 passo (30s) pra frente/atras.</summary>
    bool ValidateCode(string secret, string code);

    /// <summary>URI "otpauth://" que o app autenticador le (direto ou via QR code) pra cadastrar a conta.</summary>
    string BuildProvisioningUri(string secret, string accountEmail, string issuer);
}
