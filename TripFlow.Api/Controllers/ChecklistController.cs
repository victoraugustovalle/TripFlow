using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Checklist;
using TripFlow.Application.Checklist.DTOs;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/checklist")]
[Authorize]
public class ChecklistController : ApiControllerBase
{
    private readonly ChecklistService _checklistService;

    public ChecklistController(ChecklistService checklistService)
    {
        _checklistService = checklistService;
    }

    /// <summary>Cria um item de checklist, opcionalmente atribuido a um participante e com data limite. Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ChecklistItemDto>> Create(Guid tripId, [FromBody] CreateChecklistItemRequest request, CancellationToken ct)
    {
        return Ok(await _checklistService.CreateAsync(tripId, request, ct));
    }

    /// <summary>Lista os itens do checklist, pendentes primeiro, ordenados por data limite. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<ChecklistItemDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _checklistService.ListAsync(tripId, ct));
    }

    /// <summary>Atualiza um item do checklist (titulo, concluido, responsavel, prazo). Requer papel Editor ou Owner.</summary>
    [HttpPut("{itemId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<ActionResult<ChecklistItemDto>> Update(Guid tripId, Guid itemId, [FromBody] UpdateChecklistItemRequest request, CancellationToken ct)
    {
        return FromResult(await _checklistService.UpdateAsync(tripId, itemId, request, ct));
    }

    /// <summary>Remove um item do checklist. Requer papel Editor ou Owner.</summary>
    [HttpDelete("{itemId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid itemId, CancellationToken ct)
    {
        var result = await _checklistService.DeleteAsync(tripId, itemId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
