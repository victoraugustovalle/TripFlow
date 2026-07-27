namespace TripFlow.Application.Common;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string R2AccountId { get; set; } = string.Empty;
    public string R2AccessKeyId { get; set; } = string.Empty;
    public string R2SecretAccessKey { get; set; } = string.Empty;
    public string R2BucketName { get; set; } = string.Empty;

    /// <summary>Sem isso configurado, usa disco local (App_Data/uploads) - so pra dev, nunca em producao real.</summary>
    public bool IsR2Configured =>
        !string.IsNullOrWhiteSpace(R2AccountId) &&
        !string.IsNullOrWhiteSpace(R2AccessKeyId) &&
        !string.IsNullOrWhiteSpace(R2SecretAccessKey) &&
        !string.IsNullOrWhiteSpace(R2BucketName);
}
