using Microsoft.AspNetCore.Authorization;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.Authorization;

public class TripRoleRequirement : IAuthorizationRequirement
{
    public TripRole MinimumRole { get; }

    public TripRoleRequirement(TripRole minimumRole)
    {
        MinimumRole = minimumRole;
    }
}
