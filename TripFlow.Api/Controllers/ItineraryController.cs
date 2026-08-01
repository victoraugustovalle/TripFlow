using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Itinerary;
using TripFlow.Application.Itinerary.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/itinerary")]
[Authorize]
public class ItineraryController : ApiControllerBase
{
    private readonly ItineraryService _itineraryService;
    private readonly ItineraryWeatherService _itineraryWeatherService;

    public ItineraryController(ItineraryService itineraryService, ItineraryWeatherService itineraryWeatherService)
    {
        _itineraryService = itineraryService;
        _itineraryWeatherService = itineraryWeatherService;
    }

    /// <summary>Cria um item de roteiro (atividade, transporte, hospedagem, refeicao...) numa data e horario da viagem. Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ItineraryItemDto>> Create(Guid tripId, [FromBody] CreateItineraryItemRequest request, CancellationToken ct)
    {
        return Ok(await _itineraryService.CreateAsync(tripId, User.GetUserId(), request, ct));
    }

    /// <summary>Lista o roteiro completo da viagem, ordenado por data e horario.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ItineraryItemDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _itineraryService.ListAsync(tripId, ct));
    }

    /// <summary>Previsao do tempo dos proximos dias do roteiro que tem coordenadas, com
    /// sugestoes de item de checklist (ex.: "Guarda-chuva" se chuva e provavel).</summary>
    [HttpGet("weather")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ItineraryDayWeatherDto>>> GetWeather(Guid tripId, CancellationToken ct)
    {
        return Ok(await _itineraryWeatherService.GetForecastAsync(tripId, ct));
    }

    /// <summary>Atualiza um item de roteiro. Requer papel Editor ou Owner.</summary>
    [HttpPut("{itemId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ItineraryItemDto>> Update(Guid tripId, Guid itemId, [FromBody] UpdateItineraryItemRequest request, CancellationToken ct)
    {
        return FromResult(await _itineraryService.UpdateAsync(tripId, User.GetUserId(), itemId, request, ct));
    }

    /// <summary>Remove um item de roteiro. Se houver reserva vinculada, ela permanece, so perde o vinculo. Requer papel Editor ou Owner.</summary>
    [HttpDelete("{itemId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid itemId, CancellationToken ct)
    {
        var result = await _itineraryService.DeleteAsync(tripId, User.GetUserId(), itemId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
