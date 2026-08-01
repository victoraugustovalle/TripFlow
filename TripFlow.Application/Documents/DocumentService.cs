using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Documents.DTOs;
using TripFlow.Application.Notifications;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Documents;

public class DocumentService
{
    private readonly IAppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ActivityService _activityService;
    private readonly NotificationService _notificationService;

    public DocumentService(IAppDbContext db, IFileStorageService fileStorage, ActivityService activityService, NotificationService notificationService)
    {
        _db = db;
        _fileStorage = fileStorage;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<DocumentDto>> UploadAsync(
        Guid tripId, Guid uploadedByUserId, string fileName, string contentType, long sizeBytes,
        DocumentCategory category, Stream content, CancellationToken ct = default)
    {
        if (sizeBytes <= 0 || sizeBytes > FileValidation.MaxSizeBytes)
            return ServiceResult<DocumentDto>.Failure(ServiceErrorType.Validation, "Arquivo vazio ou maior que 10MB.");

        var header = new byte[16];
        var read = await content.ReadAsync(header.AsMemory(0, header.Length), ct);
        content.Position = 0;

        if (!FileValidation.IsAllowed(contentType, header.AsSpan(0, read)))
            return ServiceResult<DocumentDto>.Failure(ServiceErrorType.Validation, "Tipo de arquivo nao permitido. Aceito: PDF, JPEG, PNG, WEBP.");

        var participant = await _db.TripParticipants.AsNoTracking().FirstOrDefaultAsync(
            p => p.TripId == tripId && p.UserId == uploadedByUserId && p.Status == ParticipantStatus.Accepted, ct);
        if (participant is null)
            return ServiceResult<DocumentDto>.Failure(ServiceErrorType.Unauthorized, "Voce nao e participante dessa viagem.");

        var storageKey = $"trips/{tripId}/documents/{Guid.NewGuid()}-{SanitizeFileName(fileName)}";
        await _fileStorage.UploadAsync(storageKey, content, contentType, ct);

        var document = new Document
        {
            TripId = tripId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            Category = category,
            UploadedByParticipantId = participant.Id
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<DocumentDto>.Success(ToDto(document));
    }

    public async Task<PagedResult<DocumentDto>> ListAsync(Guid tripId, DocumentListQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var documentsQuery = _db.Documents.AsNoTracking().Where(d => d.TripId == tripId).OrderByDescending(d => d.CreatedAt);

        var totalCount = await documentsQuery.CountAsync(ct);
        var documents = await documentsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<DocumentDto>(documents.Select(ToDto).ToList(), totalCount, page, pageSize);
    }

    public async Task<ServiceResult<(Document Document, FileDownload Download)>> DownloadAsync(Guid tripId, Guid documentId, CancellationToken ct = default)
    {
        var document = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId && d.TripId == tripId, ct);
        if (document is null)
            return ServiceResult<(Document, FileDownload)>.Failure(ServiceErrorType.NotFound, "Documento nao encontrado.");

        var download = await _fileStorage.DownloadAsync(document.StorageKey, ct);
        if (download is null)
            return ServiceResult<(Document, FileDownload)>.Failure(ServiceErrorType.NotFound, "Arquivo nao encontrado no storage.");

        return ServiceResult<(Document, FileDownload)>.Success((document, download));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid actorUserId, Guid documentId, CancellationToken ct = default)
    {
        var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.TripId == tripId, ct);
        if (document is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Documento nao encontrado.");

        var fileName = document.FileName;

        await _fileStorage.DeleteAsync(document.StorageKey, ct);
        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(ct);

        var (_, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.DocumentDeleted, $"{actorName} removeu o documento \"{fileName}\".", ct);

        return ServiceResult<bool>.Success(true);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            name = name.Replace(invalidChar, '_');

        return name;
    }

    private static DocumentDto ToDto(Document d) => new(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.Category, d.UploadedByParticipantId, d.CreatedAt);
}
