using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TripFlow.Api.Controllers;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Documents.DTOs;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Participants.DTOs;
using TripFlow.Application.Reservations.DTOs;
using TripFlow.Application.Trips.DTOs;
using TripFlow.Domain.Enums;
using Xunit;

namespace TripFlow.Tests.Integration;

public class Phase2LifecycleTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, string Email)> RegisterAndLoginAsync(string namePrefix)
    {
        var client = _factory.CreateClient();
        var email = $"{namePrefix}-{Guid.NewGuid():N}@example.com".ToLowerInvariant();
        const string password = "SenhaForte@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(namePrefix, email, password));
        var code = _factory.EmailSender.GetCode(email, "Confirme seu e-mail");
        await client.PostAsJsonAsync("/api/auth/confirm-email", new ConfirmEmailRequest(email, code!));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var auth = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();

        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (authedClient, email);
    }

    private async Task InviteAndAcceptAsync(HttpClient owner, HttpClient invitee, string inviteeEmail, Guid tripId, TripRole role)
    {
        await owner.PostAsJsonAsync($"/api/trips/{tripId}/participants/invite", new InviteParticipantRequest(inviteeEmail, role));
        var token = _factory.EmailSender.GetCode(inviteeEmail, "Convite pra uma viagem no TripFlow");
        await invitee.PostAsJsonAsync($"/api/trips/{tripId}/participants/accept", new AcceptInviteRequest(token!));
    }

    private static async Task<Guid> CreateTripAsync(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem fase 2", "Paraty", null, null, null, "BRL"));
        var trip = await response.Content.ReadFromJsonAsync<TripDto>();
        return trip!.Id;
    }

    [Fact]
    public async Task Roteiro_CriarListarAtualizarApagar_FuncionaEBloqueiaViewer()
    {
        var (owner, _) = await RegisterAndLoginAsync("Dono");
        var (viewer, viewerEmail) = await RegisterAndLoginAsync("Visitante");
        var tripId = await CreateTripAsync(owner);
        await InviteAndAcceptAsync(owner, viewer, viewerEmail, tripId, TripRole.Viewer);

        var createRequest = new CreateItineraryItemRequest(
            "Passeio de escuna", "Volta pelas ilhas", ItineraryItemType.Activity,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(13, 0),
            "Paraty, RJ", -23.2178, -44.7131);

        // Viewer nao pode criar item de roteiro.
        var forbiddenResponse = await viewer.PostAsJsonAsync($"/api/trips/{tripId}/itinerary", createRequest);
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createResponse = await owner.PostAsJsonAsync($"/api/trips/{tripId}/itinerary", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await createResponse.Content.ReadFromJsonAsync<ItineraryItemDto>();

        // Viewer consegue ver, so nao editar.
        var listAsViewer = await viewer.GetAsync($"/api/trips/{tripId}/itinerary");
        var list = await listAsViewer.Content.ReadFromJsonAsync<List<ItineraryItemDto>>();
        list.Should().ContainSingle(i => i.Id == item!.Id);

        var updateResponse = await owner.PutAsJsonAsync($"/api/trips/{tripId}/itinerary/{item!.Id}", new UpdateItineraryItemRequest(
            "Passeio de escuna (remarcado)", item.Description, item.Type, item.ItemDate, item.StartTime, item.EndTime, item.Location, item.Latitude, item.Longitude));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await owner.DeleteAsync($"/api/trips/{tripId}/itinerary/{item.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reserva_VinculadaAoRoteiro_CriaEListaComOVinculo()
    {
        var (owner, _) = await RegisterAndLoginAsync("Dono");
        var tripId = await CreateTripAsync(owner);

        var itineraryResponse = await owner.PostAsJsonAsync($"/api/trips/{tripId}/itinerary", new CreateItineraryItemRequest(
            "Voo", null, ItineraryItemType.Transport, DateOnly.FromDateTime(DateTime.UtcNow), null, null, null, null, null));
        var item = await itineraryResponse.Content.ReadFromJsonAsync<ItineraryItemDto>();

        var reservationResponse = await owner.PostAsJsonAsync($"/api/trips/{tripId}/reservations", new CreateReservationRequest(
            ReservationType.Flight, "Voo Sao Paulo -> Paraty", "Latam", "ABC123",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null, null, null, 450.90m, "BRL", null, item!.Id));
        reservationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservation = await reservationResponse.Content.ReadFromJsonAsync<ReservationDto>();
        reservation!.ItineraryItemId.Should().Be(item.Id);

        var listResponse = await owner.GetAsync($"/api/trips/{tripId}/reservations");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ReservationDto>>();
        list.Should().ContainSingle(r => r.Id == reservation.Id);
    }

    [Fact]
    public async Task Reserva_ComItineraryItemDeOutraViagem_RetornaValidationError()
    {
        var (owner, _) = await RegisterAndLoginAsync("Dono");
        var tripId = await CreateTripAsync(owner);
        var otherTripId = await CreateTripAsync(owner);

        var itineraryResponse = await owner.PostAsJsonAsync($"/api/trips/{otherTripId}/itinerary", new CreateItineraryItemRequest(
            "Item de outra viagem", null, ItineraryItemType.Other, DateOnly.FromDateTime(DateTime.UtcNow), null, null, null, null, null));
        var itemFromOtherTrip = await itineraryResponse.Content.ReadFromJsonAsync<ItineraryItemDto>();

        var reservationResponse = await owner.PostAsJsonAsync($"/api/trips/{tripId}/reservations", new CreateReservationRequest(
            ReservationType.Other, "Reserva invalida", null, null, null, null, null, null, null, null, null, null, itemFromOtherTrip!.Id));

        reservationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Documento_UploadValido_ListaBaixaEApaga()
    {
        var (owner, _) = await RegisterAndLoginAsync("Dono");
        var tripId = await CreateTripAsync(owner);

        var pdfBytes = "%PDF-1.4\n%EOF"u8.ToArray();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "passaporte.pdf");
        content.Add(new StringContent(((int)DocumentCategory.Passport).ToString()), "category");

        var uploadResponse = await owner.PostAsync($"/api/trips/{tripId}/documents", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();
        document!.FileName.Should().Be("passaporte.pdf");

        var listResponse = await owner.GetAsync($"/api/trips/{tripId}/documents");
        var list = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();
        list.Should().ContainSingle(d => d.Id == document.Id);

        var downloadResponse = await owner.GetAsync($"/api/trips/{tripId}/documents/{document.Id}/download");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().Equal(pdfBytes);

        var deleteResponse = await owner.DeleteAsync($"/api/trips/{tripId}/documents/{document.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterDeleteResponse = await owner.GetAsync($"/api/trips/{tripId}/documents");
        var listAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();
        listAfterDelete.Should().BeEmpty();
    }

    [Fact]
    public async Task Documento_ArquivoDisfarcado_RetornaBadRequestENaoSalvaNada()
    {
        var (owner, _) = await RegisterAndLoginAsync("Dono");
        var tripId = await CreateTripAsync(owner);

        var fakeBytes = "isso aqui nao e um pdf de verdade"u8.ToArray();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fakeBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "fake.pdf");
        content.Add(new StringContent(((int)DocumentCategory.Other).ToString()), "category");

        var uploadResponse = await owner.PostAsync($"/api/trips/{tripId}/documents", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var listResponse = await owner.GetAsync($"/api/trips/{tripId}/documents");
        var list = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Geocoding_SemToken_RetornaUnauthorized()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/geocode?query=Paraty");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
