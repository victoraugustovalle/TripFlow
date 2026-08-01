using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Reservations;
using TripFlow.Application.Reservations.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/reservations")]
[Authorize]
public class ReservationsController : ApiControllerBase
{
    private readonly ReservationService _reservationService;

    public ReservationsController(ReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    /// <summary>Cadastra uma reserva (voo, hotel, aluguel de carro...), opcionalmente vinculada a um item do roteiro. Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ReservationDto>> Create(Guid tripId, [FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        return FromResult(await _reservationService.CreateAsync(tripId, User.GetUserId(), request, ct));
    }

    /// <summary>Lista as reservas da viagem, ordenadas por data de inicio.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _reservationService.ListAsync(tripId, ct));
    }

    /// <summary>Atualiza uma reserva. Requer papel Editor ou Owner.</summary>
    [HttpPut("{reservationId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ReservationDto>> Update(Guid tripId, Guid reservationId, [FromBody] UpdateReservationRequest request, CancellationToken ct)
    {
        return FromResult(await _reservationService.UpdateAsync(tripId, User.GetUserId(), reservationId, request, ct));
    }

    /// <summary>Remove uma reserva. Requer papel Editor ou Owner.</summary>
    [HttpDelete("{reservationId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid reservationId, CancellationToken ct)
    {
        var result = await _reservationService.DeleteAsync(tripId, User.GetUserId(), reservationId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
