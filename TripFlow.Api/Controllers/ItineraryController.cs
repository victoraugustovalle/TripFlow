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
    private readonly ItineraryProposalService _itineraryProposalService;

    public ItineraryController(
        ItineraryService itineraryService, ItineraryWeatherService itineraryWeatherService, ItineraryProposalService itineraryProposalService)
    {
        _itineraryService = itineraryService;
        _itineraryWeatherService = itineraryWeatherService;
        _itineraryProposalService = itineraryProposalService;
    }

    /// <summary>Cria um item de roteiro (atividade, transporte, hospedagem, refeicao...) numa data e horario da viagem. Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ItineraryItemDto>> Create(Guid tripId, [FromBody] CreateItineraryItemRequest request, CancellationToken ct)
    {
        return Ok(await _itineraryService.CreateAsync(tripId, User.GetUserId(), request, ct));
    }

    /// <summary>Cria uma proposta com 2 a 5 opcoes concorrentes pro mesmo horario, pra o grupo
    /// votar antes de decidir. Requer papel Editor ou Owner.</summary>
    [HttpPost("proposals")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ItineraryItemDto>> CreateProposal(Guid tripId, [FromBody] CreateItineraryProposalRequest request, CancellationToken ct)
    {
        return Ok(await _itineraryProposalService.CreateAsync(tripId, User.GetUserId(), request, ct));
    }

    /// <summary>Vota (ou troca de voto) numa opcao de uma proposta em aberto. Qualquer participante pode votar.</summary>
    [HttpPost("{itemId:guid}/vote")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<ItineraryItemDto>> Vote(Guid tripId, Guid itemId, [FromBody] VoteItineraryProposalRequest request, CancellationToken ct)
    {
        return FromResult(await _itineraryProposalService.VoteAsync(tripId, User.GetUserId(), itemId, request.OptionId, ct));
    }

    /// <summary>Confirma a opcao vencedora de uma proposta - ela vira um item de roteiro normal. Requer papel Editor ou Owner.</summary>
    [HttpPost("{itemId:guid}/confirm")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ItineraryItemDto>> ConfirmProposal(
        Guid tripId, Guid itemId, [FromBody] ConfirmItineraryProposalRequest request, CancellationToken ct)
    {
        return FromResult(await _itineraryProposalService.ConfirmAsync(tripId, User.GetUserId(), itemId, request.OptionId, ct));
    }

    /// <summary>Lista o roteiro completo da viagem (itens confirmados e propostas em votacao), ordenado por data e horario.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ItineraryItemDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _itineraryService.ListAsync(tripId, User.GetUserId(), ct));
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
