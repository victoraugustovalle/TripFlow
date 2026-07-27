using TripFlow.Domain.Enums;

namespace TripFlow.Application.Documents.DTOs;

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentCategory Category,
    Guid UploadedByParticipantId,
    DateTime CreatedAt);
