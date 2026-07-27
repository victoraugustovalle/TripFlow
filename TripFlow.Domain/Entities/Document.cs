using TripFlow.Domain.Enums;

namespace TripFlow.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>Chave/caminho do arquivo no storage (bucket S3-compatible ou disco local) - nao e a URL publica.</summary>
    public required string StorageKey { get; set; }

    public DocumentCategory Category { get; set; } = DocumentCategory.Other;

    public Guid UploadedByParticipantId { get; set; }
    public TripParticipant? UploadedByParticipant { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
