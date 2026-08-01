using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;
using TripFlow.Application.Retrospective.DTOs;
using TripFlow.Domain.Entities;

namespace TripFlow.Application.Retrospective;

/// <summary>Upsert do registro pessoal de um participante sobre a viagem (melhor momento, nota,
/// foto) - sempre em nome de quem esta autenticado, nunca em nome de outro participante.</summary>
public class TripMemoryService
{
    private readonly IAppDbContext _db;

    public TripMemoryService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<TripMemoryDto>> UpsertAsync(Guid tripId, Guid actorUserId, UpsertTripMemoryRequest request, CancellationToken ct = default)
    {
        var participant = await _db.TripParticipants.FirstOrDefaultAsync(p => p.TripId == tripId && p.UserId == actorUserId, ct);
        if (participant is null)
            return ServiceResult<TripMemoryDto>.Failure(ServiceErrorType.Validation, "Voce precisa ser participante da viagem.");

        var memory = await _db.TripMemories.FirstOrDefaultAsync(m => m.TripId == tripId && m.ParticipantId == participant.Id, ct);
        if (memory is null)
        {
            memory = new TripMemory { TripId = tripId, ParticipantId = participant.Id };
            _db.TripMemories.Add(memory);
        }

        memory.Highlight = string.IsNullOrWhiteSpace(request.Highlight) ? null : request.Highlight.Trim();
        memory.Rating = request.Rating;
        memory.PhotoUrl = request.PhotoUrl;
        memory.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var displayName = participant.DisplayName ?? participant.InvitedEmail;
        return ServiceResult<TripMemoryDto>.Success(
            new TripMemoryDto(participant.Id, displayName, memory.Highlight, memory.Rating, memory.PhotoUrl, memory.UpdatedAt));
    }
}
