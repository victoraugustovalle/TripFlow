using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;

namespace TripFlow.Infrastructure.Storage;

/// <summary>
/// Cloudflare R2 - compativel com a API do S3, sem custo de egress no free tier.
/// O AmazonS3Client so precisa apontar pro endpoint do R2 em vez do endpoint padrao da AWS.
/// </summary>
public class R2FileStorageService : IFileStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public R2FileStorageService(IOptions<FileStorageOptions> options)
    {
        var config = options.Value;
        _bucketName = config.R2BucketName;

        _client = new AmazonS3Client(
            config.R2AccessKeyId,
            config.R2SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = $"https://{config.R2AccountId}.r2.cloudflarestorage.com",
                AuthenticationRegion = "auto",
                ForcePathStyle = true
            });
    }

    public async Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    public async Task<FileDownload?> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(new GetObjectRequest { BucketName = _bucketName, Key = storageKey }, cancellationToken);
            return new FileDownload(response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucketName, Key = storageKey }, cancellationToken);
    }

    public void Dispose() => _client.Dispose();
}
