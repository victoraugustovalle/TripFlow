using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;
using TripFlow.Application.Notifications.DTOs;
using TripFlow.Domain.Entities;
using TripFlow.Domain.Enums;

namespace TripFlow.Application.Notifications;

public class NotificationService
{
    private readonly IAppDbContext _db;
    private readonly ITripNotifier _tripNotifier;

    public NotificationService(IAppDbContext db, ITripNotifier tripNotifier)
    {
        _db = db;
        _tripNotifier = tripNotifier;
    }

    public async Task<NotificationsPageDto> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == userId).OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var notifications = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var tripIds = notifications.Select(n => n.TripId).Distinct().ToList();
        var tripNames = await _db.Trips.AsNoTracking()
            .Where(t => tripIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var unreadCount = await _db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);

        var items = notifications
            .Select(n => new NotificationDto(n.Id, n.TripId, tripNames.GetValueOrDefault(n.TripId, "Viagem"), n.Type, n.Message, n.IsRead, n.CreatedAt))
            .ToList();

        return new NotificationsPageDto(items, unreadCount, totalCount, page, pageSize);
    }

    public async Task<ServiceResult<bool>> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, ct);
        if (notification is null)
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Notificacao nao encontrada.");

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var notification in unread)
            notification.IsRead = true;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Cria e transmite uma notificacao pra quem estiver com essa viagem aberta agora
    /// (via SignalR) - chamado pelos services de dominio quando um evento relevante acontece
    /// (convite aceito, item de checklist atribuido...). So uso interno, nao tem controller.
    /// Nao cria nada se o destinatario silenciou esse Type nessa viagem.</summary>
    public async Task NotifyAsync(Guid tripId, Guid recipientUserId, NotificationType type, string message, CancellationToken ct = default)
    {
        var isMuted = await _db.NotificationMutes.AnyAsync(m => m.UserId == recipientUserId && m.TripId == tripId && m.Type == type, ct);
        if (isMuted)
            return;

        _db.Notifications.Add(new Notification { TripId = tripId, RecipientUserId = recipientUserId, Type = type, Message = message });
        await _db.SaveChangesAsync(ct);

        await _tripNotifier.NotifyNotificationCreatedAsync(tripId, ct);
    }

    /// <summary>Notifica todos os participantes aceitos da viagem, exceto quem disparou a acao -
    /// usado pelos eventos "de grupo" (gasto lancado, reserva criada, orcamento estourado...)
    /// onde todo mundo deveria saber, ao contrario de NotifyAsync que manda pra uma pessoa so.
    /// excludeUserId e null pra avisos proativos (job diario) que nao tem um ator humano - nesse
    /// caso ninguem fica de fora do broadcast.</summary>
    public async Task NotifyTripAsync(Guid tripId, Guid? excludeUserId, NotificationType type, string message, CancellationToken ct = default)
    {
        var recipientUserIds = await _db.TripParticipants.AsNoTracking()
            .Where(p => p.TripId == tripId && p.Status == ParticipantStatus.Accepted && p.UserId != null && p.UserId != excludeUserId)
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);

        foreach (var userId in recipientUserIds)
            await NotifyAsync(tripId, userId, type, message, ct);
    }

    public async Task<IReadOnlyList<NotificationType>> GetMutedTypesAsync(Guid tripId, Guid userId, CancellationToken ct = default)
    {
        return await _db.NotificationMutes.AsNoTracking()
            .Where(m => m.TripId == tripId && m.UserId == userId)
            .Select(m => m.Type)
            .ToListAsync(ct);
    }

    /// <summary>Substitui a lista inteira de tipos silenciados pra essa viagem - mais simples pro
    /// frontend (manda o estado final do formulario de preferencias) do que expor add/remove.</summary>
    public async Task SetMutedTypesAsync(Guid tripId, Guid userId, IReadOnlyList<NotificationType> mutedTypes, CancellationToken ct = default)
    {
        var existing = await _db.NotificationMutes.Where(m => m.TripId == tripId && m.UserId == userId).ToListAsync(ct);
        _db.NotificationMutes.RemoveRange(existing);

        foreach (var type in mutedTypes.Distinct())
            _db.NotificationMutes.Add(new NotificationMute { TripId = tripId, UserId = userId, Type = type });

        await _db.SaveChangesAsync(ct);
    }
}
