using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;

namespace TripFlow.Infrastructure.Security;

/// <summary>
/// AES-256-GCM com nonce aleatorio por valor (nonce + tag + texto cifrado, tudo em base64).
/// Usado pro segredo TOTP, que precisa ser lido de volta em claro - diferente de senha,
/// que so precisa ser conferida (por isso senha usa Argon2id, hash de mao unica).
/// </summary>
public class AesSecretProtector : ISecretProtector
{
    private readonly TwoFactorOptions _options;

    public AesSecretProtector(IOptions<TwoFactorOptions> options)
    {
        _options = options.Value;
    }

    public string Protect(string plaintext)
    {
        var key = GetKey();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[plaintextBytes.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Unprotect(string protectedValue)
    {
        var key = GetKey();
        var data = Convert.FromBase64String(protectedValue);

        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;

        var nonce = data.AsSpan(0, nonceSize);
        var tag = data.AsSpan(nonceSize, tagSize);
        var ciphertext = data.AsSpan(nonceSize + tagSize);
        var plaintextBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private byte[] GetKey()
    {
        if (string.IsNullOrWhiteSpace(_options.EncryptionKeyBase64))
            throw new InvalidOperationException("TwoFactor:EncryptionKeyBase64 nao configurado - gere com `openssl rand -base64 32`.");

        var key = Convert.FromBase64String(_options.EncryptionKeyBase64);
        if (key.Length != 32)
            throw new InvalidOperationException("TwoFactor:EncryptionKeyBase64 precisa decodificar pra 32 bytes (AES-256).");

        return key;
    }
}
