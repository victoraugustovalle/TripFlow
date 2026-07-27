using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripFlow.Api.Authorization;
using TripFlow.Application.Documents;
using TripFlow.Application.Documents.DTOs;
using TripFlow.Domain.Enums;

namespace TripFlow.Api.Controllers;

[Route("api/trips/{tripId:guid}/documents")]
[Authorize]
public class DocumentsController : ApiControllerBase
{
    private readonly DocumentService _documentService;

    public DocumentsController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>Envia um documento (passaporte, ticket, voucher, seguro...). Aceita PDF, JPEG, PNG ou WEBP, ate 10MB - o tipo real do arquivo e conferido pelos bytes, nao so pelo nome/Content-Type declarado. Requer papel Editor ou Owner.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    [RequestSizeLimit(11_000_000)]
    public async Task<ActionResult<DocumentDto>> Upload(Guid tripId, IFormFile file, [FromForm] DocumentCategory category, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "Arquivo vazio." });

        await using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(tripId, User.GetUserId(), file.FileName, file.ContentType, file.Length, category, stream, ct);
        return FromResult(result);
    }

    /// <summary>Lista os documentos da viagem, do mais recente pro mais antigo. Requer ser participante da viagem.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(Guid tripId, CancellationToken ct)
    {
        return Ok(await _documentService.ListAsync(tripId, ct));
    }

    /// <summary>Baixa o conteudo do documento. Requer ser participante da viagem.</summary>
    [HttpGet("{documentId:guid}/download")]
    [Authorize(Policy = AuthorizationExtensions.TripViewerPolicy)]
    public async Task<IActionResult> Download(Guid tripId, Guid documentId, CancellationToken ct)
    {
        var result = await _documentService.DownloadAsync(tripId, documentId, ct);
        if (!result.Succeeded)
            return FromResult(result).Result!;

        var (document, download) = result.Value;
        return File(download.Content, download.ContentType, document.FileName);
    }

    /// <summary>Remove um documento (e o arquivo no storage). Requer papel Editor ou Owner.</summary>
    [HttpDelete("{documentId:guid}")]
    [Authorize(Policy = AuthorizationExtensions.TripEditorPolicy)]
    public async Task<IActionResult> Delete(Guid tripId, Guid documentId, CancellationToken ct)
    {
        var result = await _documentService.DeleteAsync(tripId, documentId, ct);
        return result.Succeeded ? NoContent() : FromResult(result).Result!;
    }
}
