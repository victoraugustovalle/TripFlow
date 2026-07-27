using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TripFlow.Api.Authorization;
using TripFlow.Application.Abstractions;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.RealTime;

/// <summary>
/// Um grupo do SignalR por viagem - so entra quem for participante aceito (checado aqui na
/// entrada do grupo, ja que Hub nao passa pelo mesmo pipeline de rota/policy dos controllers).
/// </summary>
[Authorize]
public class TripHub : Hub<ITripHubClient>
{
    private readonly IAppDbContext _db;

    public TripHub(IAppDbContext db)
    {
        _db = db;
    }

    public async Task JoinTrip(Guid tripId)
    {
        var userId = Context.User!.GetUserId();

        var isParticipant = await _db.TripParticipants.AsNoTracking().AnyAsync(p =>
            p.TripId == tripId && p.UserId == userId && p.Status == ParticipantStatus.Accepted);

        if (!isParticipant)
            throw new HubException("Voce nao e participante dessa viagem.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tripId));
    }

    public async Task LeaveTrip(Guid tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(tripId));
    }

    public static string GroupName(Guid tripId) => $"trip-{tripId}";
}
