using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.Itinerary;

public class ItineraryService
{
    private readonly IAppDbContext _db;

    public ItineraryService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ItineraryItemDto> CreateAsync(Guid tripId, CreateItineraryItemRequest request, CancellationToken ct = default)
    {
        var item = new ItineraryItem
        {
            TripId = tripId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Type = request.Type,
            ItemDate = request.ItemDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.ItineraryItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<ItineraryItemDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var items = await _db.ItineraryItems.AsNoTracking()
            .Where(i => i.TripId == tripId)
            .OrderBy(i => i.ItemDate).ThenBy(i => i.StartTime)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<ItineraryItemDto>> UpdateAsync(Guid tripId, Guid itemId, UpdateItineraryItemRequest request, CancellationToken ct = default)
    {
        var item = await _db.ItineraryItems.FirstOrDefaultAsync(i => i.Id == itemId && i.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<ItineraryItemDto>.Failure(ServiceErrorType.NotFound, "Item de roteiro nao encontrado.");

        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.Type = request.Type;
        item.ItemDate = request.ItemDate;
        item.StartTime = request.StartTime;
        item.EndTime = request.EndTime;
        item.Location = request.Location?.Trim();
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;

        await _db.SaveChangesAsync(ct);
        return ServiceResult<ItineraryItemDto>.Success(ToDto(item));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.ItineraryItems.FirstOrDefaultAsync(i => i.Id == itemId && i.TripId == tripId, ct);
        if (item is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Item de roteiro nao encontrado.");

        _db.ItineraryItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    private static ItineraryItemDto ToDto(ItineraryItem item) => new(
        item.Id, item.Title, item.Description, item.Type, item.ItemDate, item.StartTime, item.EndTime,
        item.Location, item.Latitude, item.Longitude, item.CreatedAt);
}
