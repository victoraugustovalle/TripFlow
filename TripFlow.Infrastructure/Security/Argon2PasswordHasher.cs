using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using TripFlow.Application.Abstractions;

namespace TripFlow.Infrastructure.Security;

/// <summary>
/// Argon2id (vencedor da Password Hashing Competition, recomendacao atual da OWASP).
/// Formato de armazenamento: {iterations}.{memoryKb}.{parallelism}.{saltBase64}.{hashBase64}
/// - guarda os parametros junto do hash pra poder mudar o custo no futuro sem invalidar
/// senhas ja cadastradas com os parametros antigos.
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 3;
    private const int MemoryKb = 65536; // 64 MB
    private const int Parallelism = 2;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt, Iterations, MemoryKb, Parallelism);

        return string.Join('.',
            Iterations,
            MemoryKb,
            Parallelism,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 5)
            return false;

        if (!int.TryParse(parts[0], out var iterations) ||
            !int.TryParse(parts[1], out var memoryKb) ||
            !int.TryParse(parts[2], out var parallelism))
            return false;

        var salt = Convert.FromBase64String(parts[3]);
        var expectedHash = Convert.FromBase64String(parts[4]);

        var actualHash = ComputeHash(password, salt, iterations, memoryKb, parallelism, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int iterations, int memoryKb, int parallelism, int hashSize = HashSize)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            Iterations = iterations,
            MemorySize = memoryKb
        };

        return argon2.GetBytes(hashSize);
    }
}
