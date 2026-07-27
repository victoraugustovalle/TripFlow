using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Common;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.Checklist;

public class ChecklistService
{
    private readonly IAppDbContext _db;

    public ChecklistService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ChecklistItemDto> CreateAsync(Guid tripId, CreateChecklistItemRequest request, CancellationToken ct = default)
    {
        var item = new ChecklistItem
        {
            TripId = tripId,
            Title = request.Title.Trim(),
            AssignedToParticipantId = request.AssignedToParticipantId,
            DueDate = request.DueDate
        };

        _db.ChecklistItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<ChecklistItemDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var items = await _db.ChecklistItems.AsNoTracking()
            .Where(c => c.TripId == tripId)
            .OrderBy(c => c.IsDone).ThenBy(c => c.DueDate)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<ChecklistItemDto>> UpdateAsync(Guid tripId, Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        var item = await _db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<ChecklistItemDto>.Failure(ServiceErrorType.NotFound, "Item nao encontrado.");

        item.Title = request.Title.Trim();
        item.IsDone = request.IsDone;
        item.AssignedToParticipantId = request.AssignedToParticipantId;
        item.DueDate = request.DueDate;

        await _db.SaveChangesAsync(ct);
        return ServiceResult<ChecklistItemDto>.Success(ToDto(item));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.ChecklistItems.FirstOrDefaultAsync(c => c.Id == itemId && c.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Item nao encontrado.");

        _db.ChecklistItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    private static ChecklistItemDto ToDto(ChecklistItem item) => new(item.Id, item.Title, item.IsDone, item.AssignedToParticipantId, item.DueDate, item.CreatedAt);
}
