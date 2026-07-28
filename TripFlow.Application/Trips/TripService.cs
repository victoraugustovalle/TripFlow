using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;
using TripFlow.Application.Trips.DTOs;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Trips;

public class TripService
{
    private readonly IAppDbContext _db;

    public TripService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<TripDto> CreateAsync(Guid currentUserId, string currentUserEmail, string currentUserName, CreateTripRequest request, CancellationToken ct = default)
    {
        var trip = new Trip
        {
            Name = request.Name.Trim(),
            Destination = request.Destination?.Trim(),
            Description = request.Description?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            CreatedByUserId = currentUserId
        };

        trip.Participants.Add(new TripParticipant
        {
            TripId = trip.Id,
            UserId = currentUserId,
            InvitedEmail = currentUserEmail.Trim().ToLowerInvariant(),
            DisplayName = currentUserName.Trim(),
            Role = TripRole.Owner,
            Status = ParticipantStatus.Accepted,
            RespondedAt = DateTime.UtcNow
        });

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync(ct);

        return ToDto(trip);
    }

    public async Task<ServiceResult<TripDto>> GetByIdAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct);
        return trip is null
            ? ServiceResult<TripDto>.Failure(ServiceErrorType.NotFound, "Viagem nao encontrada.")
            : ServiceResult<TripDto>.Success(ToDto(trip));
    }

    public async Task<IReadOnlyList<TripSummaryDto>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var participations = await _db.TripParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == ParticipantStatus.Accepted)
            .Include(p => p.Trip)
            .ToListAsync(ct);

        return participations
            .Where(p => p.Trip is not null)
            .Select(p => new TripSummaryDto(p.Trip!.Id, p.Trip.Name, p.Trip.Destination, p.Trip.Status, p.Trip.StartDate, p.Trip.EndDate, p.Role))
            .ToList();
    }

    public async Task<ServiceResult<TripDto>> UpdateAsync(Guid tripId, UpdateTripRequest request, CancellationToken ct = default)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
            return ServiceResult<TripDto>.Failure(ServiceErrorType.NotFound, "Viagem nao encontrada.");

        trip.Name = request.Name.Trim();
        trip.Destination = request.Destination?.Trim();
        trip.Description = request.Description?.Trim();
        trip.StartDate = request.StartDate;
        trip.EndDate = request.EndDate;
        trip.Status = request.Status;

        await _db.SaveChangesAsync(ct);
        return ServiceResult<TripDto>.Success(ToDto(trip));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, CancellationToken ct = default)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Viagem nao encontrada.");

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    private static TripDto ToDto(Trip trip) => new(
        trip.Id, trip.Name, trip.Destination, trip.Description, trip.StartDate, trip.EndDate,
        trip.Currency, trip.Status, trip.CreatedByUserId, trip.CreatedAt);
}
