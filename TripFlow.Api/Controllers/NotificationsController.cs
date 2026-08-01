using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Notifications;
using TripFlow.Application.Notifications.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Lista as notificacoes do usuario autenticado, entre todas as viagens que participa, paginado e junto da contagem de nao lidas.</summary>
    [HttpGet]
    public async Task<ActionResult<NotificationsPageDto>> List([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        return Ok(await _notificationService.ListAsync(User.GetUserId(), page, pageSize, ct));
    }

    /// <summary>Marca uma notificacao como lida.</summary>
    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken ct)
    {
        var result = await _notificationService.MarkAsReadAsync(User.GetUserId(), notificationId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }

    /// <summary>Marca todas as notificacoes do usuario autenticado como lidas.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        await _notificationService.MarkAllAsReadAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
