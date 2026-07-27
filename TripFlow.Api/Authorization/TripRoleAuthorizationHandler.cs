using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TripFlow.Application.Abstractions;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.Authorization;

/// <summary>
/// Autorizacao por recurso: o papel que importa nao e o global (Admin/User), e o papel
/// do usuario DENTRO da viagem apontada pela rota (Owner/Editor/Viewer). Le o "tripId" da
/// rota atual via IHttpContextAccessor - funciona igual pra qualquer action que tenha
/// esse parametro, sem precisar repetir a consulta em cada controller.
/// </summary>
public class TripRoleAuthorizationHandler : AuthorizationHandler<TripRoleRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAppDbContext _db;

    public TripRoleAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IAppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TripRoleRequirement requirement)
    {
        if (context.User.FindFirst(ClaimTypes.Role)?.Value == GlobalRole.Admin.ToString())
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return;

        var routeValue = _httpContextAccessor.HttpContext?.Request.RouteValues["tripId"]?.ToString();
        if (routeValue is null || !Guid.TryParse(routeValue, out var tripId))
            return;

        var participant = await _db.TripParticipants.AsNoTracking().FirstOrDefaultAsync(p =>
            p.TripId == tripId && p.UserId == userId && p.Status == ParticipantStatus.Accepted);

        if (participant is not null && participant.Role >= requirement.MinimumRole)
            context.Succeed(requirement);
    }
}
