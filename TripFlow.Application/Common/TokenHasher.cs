using System.Security.Cryptography;
using System.Text;

namespace TripFlow.Application.Common;

/// <summary>
/// SHA-256 simples pra valores ja aleatorios de alta entropia (codigo de confirmacao,
/// token de reset). Nao serve pra senha - senha usa Argon2id (custo computacional
/// proposital contra brute force, o que SHA-256 puro nao oferece).
/// </summary>
public static class TokenHasher
{
    public static string Hash(string rawValue)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawValue));
        return Convert.ToHexString(bytes);
    }

    public static bool Matches(string rawValue, string storedHash)
    {
        var computed = Hash(rawValue);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(storedHash));
    }

    public static string GenerateNumericCode(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString(new string('0', digits));
    }

    public static string GenerateUrlSafeToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
