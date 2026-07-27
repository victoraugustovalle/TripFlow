namespace TripFlow.Application.Documents;

/// <summary>
/// Valida o arquivo pelos primeiros bytes (magic number), nao so pelo Content-Type que o
/// cliente declarou - um upload malicioso pode mandar qualquer Content-Type junto de um
/// arquivo que na verdade e outra coisa (ex: um .html disfarcado de .jpg).
/// </summary>
public static class FileValidation
{
    public const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly IReadOnlyDictionary<string, byte[][]> AllowedSignatures = new Dictionary<string, byte[][]>
    {
        ["application/pdf"] = [[0x25, 0x50, 0x44, 0x46]], // %PDF
        ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
        ["image/png"] = [[0x89, 0x50, 0x4E, 0x47]],
        ["image/webp"] = [[0x52, 0x49, 0x46, 0x46]] // RIFF (o "WEBP" vem depois do tamanho, nos bytes 8-11 - checado à parte)
    };

    public static bool IsAllowed(string contentType, ReadOnlySpan<byte> header)
    {
        if (!AllowedSignatures.TryGetValue(contentType, out var signatures))
            return false;

        foreach (var signature in signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                if (contentType == "image/webp")
                    return header.Length >= 12 && header.Slice(8, 4).SequenceEqual("WEBP"u8);

                return true;
            }
        }

        return false;
    }
}
