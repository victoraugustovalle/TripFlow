using TripFlow.Domain.Enums;

namespace TripFlow.Application.Notifications.DTOs;

public record NotificationDto(Guid Id, Guid TripId, string TripName, NotificationType Type, string Message, bool IsRead, DateTime CreatedAt);

public record NotificationsPageDto(IReadOnlyList<NotificationDto> Items, int UnreadCount, int TotalCount, int Page, int PageSize);

public record SetMutedNotificationTypesRequest(IReadOnlyList<NotificationType> MutedTypes);
