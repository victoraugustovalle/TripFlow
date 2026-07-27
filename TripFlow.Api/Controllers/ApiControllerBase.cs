using Microsoft.AspNetCore.Mvc;
using TripFlow.Application.Common;

namespace TripFlow.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<T> FromResult<T>(ServiceResult<T> result)
    {
        if (result.Succeeded)
            return Ok(result.Value);

        return result.Error!.Type switch
        {
            ServiceErrorType.NotFound => NotFound(new { message = result.Error.Message }),
            ServiceErrorType.Conflict => Conflict(new { message = result.Error.Message }),
            ServiceErrorType.Unauthorized => Unauthorized(new { message = result.Error.Message }),
            ServiceErrorType.LockedOut => StatusCode(StatusCodes.Status423Locked, new { message = result.Error.Message }),
            _ => BadRequest(new { message = result.Error.Message })
        };
    }
}
