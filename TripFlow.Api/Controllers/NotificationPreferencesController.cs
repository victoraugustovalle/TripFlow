using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Notifications;
using TripFlow.Application.Notifications.DTOs;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ApiControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationPreferencesController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Lista os tipos de notificacao que o usuario autenticado silenciou nessa viagem. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<NotificationType>>> Get(Guid tripId, CancellationToken ct)
    {
        return Ok(await _notificationService.GetMutedTypesAsync(tripId, User.GetUserId(), ct));
    }

    /// <summary>Substitui a lista de tipos silenciados nessa viagem pelo usuario autenticado. Requer ser participante da viagem.</summary>
    [HttpPut]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<IActionResult> Set(Guid tripId, [FromBody] SetMutedNotificationTypesRequest request, CancellationToken ct)
    {
        await _notificationService.SetMutedTypesAsync(tripId, User.GetUserId(), request.MutedTypes, ct);
        return NoContent();
    }
}
