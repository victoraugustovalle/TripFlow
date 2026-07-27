using OtpNet;
using TripFlow.Application.Abstractions;

namespace TripFlow.Infrastructure.Security;

public class TotpService : ITotpService
{
    private const int SecretSizeBytes = 20; // 160 bits - padrao recomendado pro TOTP (RFC 4226/6238)

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(SecretSizeBytes);
        return Base32Encoding.ToString(key);
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var totp = new Totp(Base32Encoding.ToBytes(secret));

        // Tolerancia de 1 passo (30s) pra frente e pra tras - absorve pequena diferenca de
        // relogio entre o celular e o servidor sem abrir demais a janela de validade.
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }

    public string BuildProvisioningUri(string secret, string accountEmail, string issuer)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountEmail}");
        var encodedIssuer = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }
}
