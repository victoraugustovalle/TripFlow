using System.Collections.Concurrent;

namespace TripFlow.Api.RealTime;

public record PresenceUser(Guid UserId, string DisplayName);

/// <summary>
/// Rastreia, em memoria, quem esta com qual viagem aberta agora - efemero por natureza (zera
/// a cada reinicio do processo), entao fica na Api como singleton em vez de virar uma entidade
/// de dominio persistida. Uma pessoa pode ter mais de uma conexao (duas abas abertas); so sai
/// da lista de presenca quando a ULTIMA conexao dela naquela viagem cai.
/// </summary>
public class TripPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, PresenceUser>> _byTrip = new();
    private readonly ConcurrentDictionary<string, Guid> _tripByConnection = new();

    public IReadOnlyList<PresenceUser> Join(Guid tripId, string connectionId, Guid userId, string displayName)
    {
        var connections = _byTrip.GetOrAdd(tripId, _ => new ConcurrentDictionary<string, PresenceUser>());
        connections[connectionId] = new PresenceUser(userId, displayName);
        _tripByConnection[connectionId] = tripId;

        return Snapshot(connections);
    }

    /// <summary>Retorna a viagem que essa conexao deixou e quem ainda esta online nela - null se
    /// a conexao nao estava associada a nenhuma viagem (ex: OnDisconnected antes de JoinTrip).</summary>
    public (Guid TripId, IReadOnlyList<PresenceUser> Remaining)? Leave(string connectionId)
    {
        if (!_tripByConnection.TryRemove(connectionId, out var tripId))
            return null;

        if (!_byTrip.TryGetValue(tripId, out var connections))
            return (tripId, []);

        connections.TryRemove(connectionId, out _);
        return (tripId, Snapshot(connections));
    }

    private static IReadOnlyList<PresenceUser> Snapshot(ConcurrentDictionary<string, PresenceUser> connections) =>
        connections.Values.DistinctBy(u => u.UserId).ToList();
}
