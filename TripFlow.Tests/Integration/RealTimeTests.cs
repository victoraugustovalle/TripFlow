using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TripFlow.Api.Controllers;
using TripFlow.Api.RealTime;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Participants.DTOs;
using TripFlow.Application.Trips.DTOs;
using TripFlow.Domain.Enums;
using Xunit;

namespace TripFlow.Tests.Integration;

[Collection("Integration")]
public class RealTimeTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, string Email, string AccessToken)> RegisterAndLoginAsync(string namePrefix)
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
        return (authedClient, email, auth.AccessToken);
    }

    private HubConnection BuildConnection(string accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, RealTimeExtensions.HubPath), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
    }

    [Fact]
    public async Task ExpenseCriado_ParticipanteConectadoRecebeEventoEmTempoReal()
    {
        var (owner, _, ownerToken) = await RegisterAndLoginAsync("Dono");
        var (guest, guestEmail, guestToken) = await RegisterAndLoginAsync("Convidado");

        var tripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem tempo real", null, null, null, null, "BRL"));
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        await owner.PostAsJsonAsync($"/api/trips/{trip!.Id}/participants/invite", new InviteParticipantRequest(guestEmail, TripRole.Editor));
        var inviteToken = _factory.EmailSender.GetCode(guestEmail, "Convite pra uma viagem no TripFlow");
        var acceptResponse = await guest.PostAsJsonAsync($"/api/trips/{trip.Id}/participants/accept", new AcceptInviteRequest(inviteToken!));
        var guestParticipant = await acceptResponse.Content.ReadFromJsonAsync<ParticipantDto>();

        await using var guestConnection = BuildConnection(guestToken);
        var expenseReceived = new TaskCompletionSource<ExpenseDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        guestConnection.On<ExpenseDto>("ExpenseCreated", dto => expenseReceived.TrySetResult(dto));

        await guestConnection.StartAsync();
        await guestConnection.InvokeAsync("JoinTrip", trip.Id);

        var createExpenseResponse = await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/expenses", new CreateExpenseRequest(
            "Jantar", 100m, "Alimentacao", guestParticipant!.Id, DateOnly.FromDateTime(DateTime.UtcNow), null));
        createExpenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdExpense = await createExpenseResponse.Content.ReadFromJsonAsync<ExpenseDto>();

        var completed = await Task.WhenAny(expenseReceived.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().Be(expenseReceived.Task, "o evento deveria ter chegado dentro do timeout");

        var received = await expenseReceived.Task;
        received.Id.Should().Be(createdExpense!.Id);
        received.Description.Should().Be("Jantar");
    }

    [Fact]
    public async Task ChecklistItemMarcado_ParticipanteConectadoRecebeAtualizacao()
    {
        var (owner, _, ownerToken) = await RegisterAndLoginAsync("Dono");
        var tripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem checklist", null, null, null, null, "BRL"));
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        var createItemResponse = await owner.PostAsJsonAsync($"/api/trips/{trip!.Id}/checklist", new CreateChecklistItemRequest("Levar passaporte", null, null));
        var item = await createItemResponse.Content.ReadFromJsonAsync<ChecklistItemDto>();

        await using var ownerConnection = BuildConnection(ownerToken);
        var updateReceived = new TaskCompletionSource<ChecklistItemDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        ownerConnection.On<ChecklistItemDto>("ChecklistItemUpdated", dto => updateReceived.TrySetResult(dto));

        await ownerConnection.StartAsync();
        await ownerConnection.InvokeAsync("JoinTrip", trip.Id);

        await owner.PutAsJsonAsync($"/api/trips/{trip.Id}/checklist/{item!.Id}", new UpdateChecklistItemRequest("Levar passaporte", true, null, null));

        var completed = await Task.WhenAny(updateReceived.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().Be(updateReceived.Task, "o evento deveria ter chegado dentro do timeout");

        var received = await updateReceived.Task;
        received.Id.Should().Be(item.Id);
        received.IsDone.Should().BeTrue();
    }

    [Fact]
    public async Task JoinTrip_UsuarioNaoParticipante_LancaHubException()
    {
        var (owner, _, _) = await RegisterAndLoginAsync("Dono");
        var (_, _, outsiderToken) = await RegisterAndLoginAsync("ForaDaViagem");

        var tripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem privada", null, null, null, null, "BRL"));
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        await using var outsiderConnection = BuildConnection(outsiderToken);
        await outsiderConnection.StartAsync();

        var act = async () => await outsiderConnection.InvokeAsync("JoinTrip", trip!.Id);

        await act.Should().ThrowAsync<HubException>();
    }
}
