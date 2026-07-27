namespace TripFlow.Application.Abstractions;

public record FileDownload(Stream Content, string ContentType);

/// <summary>
/// Abstrai onde o arquivo fica de verdade - implementacao real usa um bucket S3-compatible
/// (Cloudflare R2), com fallback pra disco local quando nao ha credencial configurada (dev).
/// A Api sempre baixa via stream atraves dela mesma (nao expoe URL direta do bucket) - assim
/// a autorizacao por papel na viagem e checada antes de qualquer byte sair.
/// </summary>
public interface IFileStorageService
{
    Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<FileDownload?> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
