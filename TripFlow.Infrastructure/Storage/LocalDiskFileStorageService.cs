using Microsoft.Extensions.Logging;
using TripFlow.Application.Abstractions;

namespace TripFlow.Infrastructure.Storage;

/// <summary>
/// Fallback pra dev local, sem precisar de conta na Cloudflare pra rodar o projeto.
/// Nunca deveria ser usado em producao real (sem redundancia, some se o volume do
/// container for recriado) - so entra quando FileStorageOptions.IsR2Configured for false.
/// </summary>
public class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<LocalDiskFileStorageService> _logger;

    public LocalDiskFileStorageService(ILogger<LocalDiskFileStorageService> logger)
    {
        _logger = logger;
        _rootPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
        Directory.CreateDirectory(_rootPath);
        _logger.LogWarning("R2 nao configurado - documentos vao pro disco local em {Path}. So use isso em dev.", _rootPath);
    }

    public async Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public async Task<FileDownload?> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            return null;

        var memoryStream = new MemoryStream();
        await using (var fileStream = File.OpenRead(path))
        {
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
        }
        memoryStream.Position = 0;

        return new FileDownload(memoryStream, GuessContentType(path));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var normalized = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));

        if (!fullPath.StartsWith(_rootPath, StringComparison.Ordinal))
            throw new InvalidOperationException("Chave de storage invalida.");

        return fullPath;
    }

    private static string GuessContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
