using TripFlow.Domain.Enums;

namespace TripFlow.Application.Documents.DTOs;

public record DocumentListQuery(int Page = 1, int PageSize = 20);

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentCategory Category,
    Guid UploadedByParticipantId,
    DateTime CreatedAt);
