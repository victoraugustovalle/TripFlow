using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Activity;
using TripFlow.Application.Common;
using TripFlow.Application.Notifications;
using TripFlow.Application.Reservations.DTOs;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Reservations;

public class ReservationService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;
    private readonly ActivityService _activityService;
    private readonly NotificationService _notificationService;

    public ReservationService(IAppDbContext db, ITripNotifier tripNotifier, ActivityService activityService, NotificationService notificationService)
    {
        _db = db;
        _tripNotifier = tripNotifier;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<ReservationDto>> CreateAsync(Guid tripId, Guid actorUserId, CreateReservationRequest request, CancellationToken ct = default)
    {
        if (request.ItineraryItemId is not null && !await ItineraryItemBelongsToTripAsync(tripId, request.ItineraryItemId.Value, ct))
            return ServiceResult<ReservationDto>.Failure(ServiceErrorType.Validation, "O item de roteiro informado nao pertence a essa viagem.");

        var reservation = new Reservation
        {
            TripId = tripId,
            Type = request.Type,
            Title = request.Title.Trim(),
            ProviderName = request.ProviderName?.Trim(),
            ConfirmationCode = request.ConfirmationCode?.Trim(),
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Location = request.Location?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Price = request.Price,
            Currency = request.Currency?.Trim().ToUpperInvariant(),
            Notes = request.Notes?.Trim(),
            ItineraryItemId = request.ItineraryItemId
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(ct);

        var dto = ToDto(reservation);
        await _tripNotifier.NotifyReservationCreatedAsync(tripId, dto, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ReservationCreated, nameof(Reservation), reservation.Id,
            $"{actorName} adicionou a reserva \"{reservation.Title}\".", ct);

        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.ReservationCreated, $"{actorName} adicionou a reserva \"{reservation.Title}\".", ct);

        return ServiceResult<ReservationDto>.Success(dto);
    }

    public async Task<IReadOnlyList<ReservationDto>> ListAsync(Guid tripId, CancellationToken ct = default)
    {
        var reservations = await _db.Reservations.AsNoTracking()
            .Where(r => r.TripId == tripId)
            .OrderBy(r => r.StartAt)
            .ToListAsync(ct);

        return reservations.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<ReservationDto>> UpdateAsync(Guid tripId, Guid actorUserId, Guid reservationId, UpdateReservationRequest request, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.TripId == tripId, ct);
        if (reservation is null)
            return ServiceResult<ReservationDto>.Failure(ServiceErrorType.NotFound, "Reserva nao encontrada.");

        if (request.ItineraryItemId is not null && !await ItineraryItemBelongsToTripAsync(tripId, request.ItineraryItemId.Value, ct))
            return ServiceResult<ReservationDto>.Failure(ServiceErrorType.Validation, "O item de roteiro informado nao pertence a essa viagem.");

        reservation.Type = request.Type;
        reservation.Title = request.Title.Trim();
        reservation.ProviderName = request.ProviderName?.Trim();
        reservation.ConfirmationCode = request.ConfirmationCode?.Trim();
        reservation.StartAt = request.StartAt;
        reservation.EndAt = request.EndAt;
        reservation.Location = request.Location?.Trim();
        reservation.Latitude = request.Latitude;
        reservation.Longitude = request.Longitude;
        reservation.Price = request.Price;
        reservation.Currency = request.Currency?.Trim().ToUpperInvariant();
        reservation.Notes = request.Notes?.Trim();
        reservation.ItineraryItemId = request.ItineraryItemId;

        await _db.SaveChangesAsync(ct);

        var dto = ToDto(reservation);
        await _tripNotifier.NotifyReservationUpdatedAsync(tripId, dto, ct);

        var (_, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _notificationService.NotifyTripAsync(tripId, actorUserId, NotificationType.ReservationUpdated, $"{actorName} atualizou a reserva \"{reservation.Title}\".", ct);

        return ServiceResult<ReservationDto>.Success(dto);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid tripId, Guid actorUserId, Guid reservationId, CancellationToken ct = default)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId && r.TripId == tripId, ct);
        if (reservation is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Reserva nao encontrada.");

        var title = reservation.Title;

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifyReservationDeletedAsync(tripId, reservationId, ct);

        var (actorParticipantId, actorName) = await _activityService.ResolveActorAsync(tripId, actorUserId, ct);
        await _activityService.RecordAsync(tripId, actorParticipantId, actorUserId, ActivityType.ReservationDeleted, nameof(Reservation), reservationId,
            $"{actorName} removeu a reserva \"{title}\".", ct);

        return ServiceResult<bool>.Success(true);
    }

    private Task<bool> ItineraryItemBelongsToTripAsync(Guid tripId, Guid itineraryItemId, CancellationToken ct) =>
        _db.ItineraryItems.AnyAsync(i => i.Id == itineraryItemId && i.TripId == tripId, ct);

    private static ReservationDto ToDto(Reservation r) => new(
        r.Id, r.Type, r.Title, r.ProviderName, r.ConfirmationCode, r.StartAt, r.EndAt,
        r.Location, r.Latitude, r.Longitude, r.Price, r.Currency, r.Notes, r.ItineraryItemId, r.CreatedAt);
}
