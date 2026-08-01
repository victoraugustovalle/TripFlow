using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TripFlow.Api.Controllers;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Budgets.DTOs;
using TripFlow.Application.Checklist.DTOs;
using TripFlow.Application.Common;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Itinerary.DTOs;
using TripFlow.Application.Notifications.DTOs;
using TripFlow.Application.Participants.DTOs;
using TripFlow.Application.Retrospective.DTOs;
using TripFlow.Application.Trips.DTOs;
using TripFlow.Domain.Enums;
using Xunit;

namespace TripFlow.Tests.Integration;

[Collection("Integration")]
public class TripLifecycleTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, string Email)> RegisterAndLoginAsync(HttpClient client, string namePrefix)
    {
        var email = $"{namePrefix}-{Guid.NewGuid():N}@example.com".ToLowerInvariant();
        const string password = "SenhaForte@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(namePrefix, email, password));
        var code = _factory.EmailSender.GetCode(email, "Confirme seu e-mail");
        await client.PostAsJsonAsync("/api/auth/confirm-email", new ConfirmEmailRequest(email, code!));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Auth!.AccessToken);

        return (authedClient, email);
    }

    [Fact]
    public async Task CriarViagem_ConvidarAceitar_LancarGastoEVerSettlement()
    {
        var (owner, _) = await RegisterAndLoginAsync(_factory.CreateClient(), "Dono");
        var (guest, guestEmail) = await RegisterAndLoginAsync(_factory.CreateClient(), "Convidado");

        var createTripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem de teste", "Serra da Mantiqueira", null, null, null, "BRL"));
        createTripResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var trip = await createTripResponse.Content.ReadFromJsonAsync<TripDto>();

        // Sem ser participante, o convidado nao consegue ver a viagem ainda.
        var forbiddenBeforeInvite = await guest.GetAsync($"/api/trips/{trip!.Id}");
        forbiddenBeforeInvite.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var inviteResponse = await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/participants/invite", new InviteParticipantRequest(guestEmail, TripRole.Editor));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var inviteToken = _factory.EmailSender.GetCode(guestEmail, "Convite pra uma viagem no TripFlow");
        inviteToken.Should().NotBeNullOrEmpty();

        var acceptResponse = await guest.PostAsJsonAsync($"/api/trips/{trip.Id}/participants/accept", new AcceptInviteRequest(inviteToken!));
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var guestParticipant = await acceptResponse.Content.ReadFromJsonAsync<ParticipantDto>();

        var participantsResponse = await owner.GetAsync($"/api/trips/{trip.Id}/participants");
        var participants = await participantsResponse.Content.ReadFromJsonAsync<List<ParticipantDto>>();
        participants.Should().HaveCount(2);
        var ownerParticipant = participants!.Single(p => p.Id != guestParticipant!.Id);

        var createExpenseResponse = await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/expenses", new CreateExpenseRequest(
            "Hospedagem", 200m, "Hospedagem", ownerParticipant.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, null));
        createExpenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var expense = await createExpenseResponse.Content.ReadFromJsonAsync<ExpenseDto>();
        expense!.Splits.Should().HaveCount(2);
        expense.Splits.Sum(s => s.ShareAmount).Should().Be(200m);

        var settlementResponse = await guest.GetAsync($"/api/trips/{trip.Id}/expenses/settlement");
        settlementResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var settlement = await settlementResponse.Content.ReadFromJsonAsync<SettlementDto>();

        settlement!.Transfers.Should().ContainSingle();
        var transfer = settlement.Transfers.Single();
        transfer.FromParticipantId.Should().Be(guestParticipant!.Id);
        transfer.ToParticipantId.Should().Be(ownerParticipant.Id);
        transfer.Amount.Should().Be(100m);

        // Viewer/Editor nao consegue apagar a viagem - so o Owner.
        var deleteAsGuest = await guest.DeleteAsync($"/api/trips/{trip.Id}");
        deleteAsGuest.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var deleteAsOwner = await owner.DeleteAsync($"/api/trips/{trip.Id}");
        deleteAsOwner.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LancarGastoComDivisaoCustomizada_RespeitaValoresInformados()
    {
        var (owner, _) = await RegisterAndLoginAsync(_factory.CreateClient(), "Dono");
        var (guest, guestEmail) = await RegisterAndLoginAsync(_factory.CreateClient(), "Convidado");

        var createTripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem com split customizado", null, null, null, null, "BRL"));
        var trip = await createTripResponse.Content.ReadFromJsonAsync<TripDto>();

        var inviteResponse = await owner.PostAsJsonAsync($"/api/trips/{trip!.Id}/participants/invite", new InviteParticipantRequest(guestEmail, TripRole.Editor));
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inviteToken = _factory.EmailSender.GetCode(guestEmail, "Convite pra uma viagem no TripFlow");

        var acceptResponse = await guest.PostAsJsonAsync($"/api/trips/{trip.Id}/participants/accept", new AcceptInviteRequest(inviteToken!));
        var guestParticipant = await acceptResponse.Content.ReadFromJsonAsync<ParticipantDto>();

        var participantsResponse = await owner.GetAsync($"/api/trips/{trip.Id}/participants");
        var participants = await participantsResponse.Content.ReadFromJsonAsync<List<ParticipantDto>>();
        var ownerParticipant = participants!.Single(p => p.Id != guestParticipant!.Id);

        // O dono pagou o jantar, mas o convidado nao bebeu vinho - divisao desigual, nao 50/50.
        var shares = new List<ExpenseShareInput>
        {
            new(ownerParticipant.Id, 130m),
            new(guestParticipant!.Id, 70m),
        };

        var createExpenseResponse = await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/expenses", new CreateExpenseRequest(
            "Jantar com vinho", 200m, "Alimentacao", ownerParticipant.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, shares));
        createExpenseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var expense = await createExpenseResponse.Content.ReadFromJsonAsync<ExpenseDto>();

        expense!.Splits.Should().HaveCount(2);
        expense.Splits.Single(s => s.ParticipantId == ownerParticipant.Id).ShareAmount.Should().Be(130m);
        expense.Splits.Single(s => s.ParticipantId == guestParticipant.Id).ShareAmount.Should().Be(70m);

        // Soma da divisao nao bate com o valor do gasto - precisa ser rejeitado antes de salvar nada.
        var mismatchedShares = new List<ExpenseShareInput> { new(ownerParticipant.Id, 100m), new(guestParticipant.Id, 50m) };
        var invalidResponse = await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/expenses", new CreateExpenseRequest(
            "Divisao errada", 200m, "Alimentacao", ownerParticipant.Id, DateOnly.FromDateTime(DateTime.UtcNow), null, mismatchedShares));
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConviteAceito_NotificaDono_EMarcarComoLidaFunciona()
    {
        var (owner, _) = await RegisterAndLoginAsync(_factory.CreateClient(), "Dono");
        var (guest, guestEmail) = await RegisterAndLoginAsync(_factory.CreateClient(), "Convidado");

        var createTripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem com notificacao", null, null, null, null, "BRL"));
        var trip = await createTripResponse.Content.ReadFromJsonAsync<TripDto>();

        await owner.PostAsJsonAsync($"/api/trips/{trip!.Id}/participants/invite", new InviteParticipantRequest(guestEmail, TripRole.Editor));
        var inviteToken = _factory.EmailSender.GetCode(guestEmail, "Convite pra uma viagem no TripFlow");

        // Antes do convite ser aceito, o dono nao tem notificacao nenhuma sobre essa viagem.
        var beforeAccept = await owner.GetFromJsonAsync<NotificationsPageDto>("/api/notifications");
        beforeAccept!.UnreadCount.Should().Be(0);

        var acceptResponse = await guest.PostAsJsonAsync($"/api/trips/{trip.Id}/participants/accept", new AcceptInviteRequest(inviteToken!));
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterAccept = await owner.GetFromJsonAsync<NotificationsPageDto>("/api/notifications");
        afterAccept!.UnreadCount.Should().Be(1);
        var notification = afterAccept.Items.Single();
        notification.TripId.Should().Be(trip.Id);
        notification.IsRead.Should().BeFalse();

        // O convidado nao deveria ter recebido notificacao nenhuma por aceitar o proprio convite.
        var guestNotifications = await guest.GetFromJsonAsync<NotificationsPageDto>("/api/notifications");
        guestNotifications!.UnreadCount.Should().Be(0);

        var markReadResponse = await owner.PostAsync($"/api/notifications/{notification.Id}/read", null);
        markReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterMarkRead = await owner.GetFromJsonAsync<NotificationsPageDto>("/api/notifications");
        afterMarkRead!.UnreadCount.Should().Be(0);
        afterMarkRead.Items.Single().IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Retrospectiva_ComponhaGastosOrcamentoEChecklist()
    {
        var (owner, _) = await RegisterAndLoginAsync(_factory.CreateClient(), "Dono");

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(4);
        var createTripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem pra retrospectiva", null, null, start, end, "BRL"));
        var trip = await createTripResponse.Content.ReadFromJsonAsync<TripDto>();

        var participantsResponse = await owner.GetAsync($"/api/trips/{trip!.Id}/participants");
        var ownerParticipant = (await participantsResponse.Content.ReadFromJsonAsync<List<ParticipantDto>>())!.Single();

        await owner.PutAsJsonAsync($"/api/trips/{trip.Id}/budgets", new UpsertBudgetRequest("Hospedagem", 100m));
        await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/expenses", new CreateExpenseRequest(
            "Hotel", 150m, "Hospedagem", ownerParticipant.Id, start, null, null));
        await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/checklist", new CreateChecklistItemRequest("Levar passaporte", null, null));
        await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/itinerary", new CreateItineraryItemRequest(
            "Passeio de barco", null, ItineraryItemType.Activity, start, null, null, null, null, null));

        var retrospectiveResponse = await owner.GetAsync($"/api/trips/{trip.Id}/retrospective");
        retrospectiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrospective = await retrospectiveResponse.Content.ReadFromJsonAsync<TripRetrospectiveDto>();

        retrospective!.DurationDays.Should().Be(5);
        retrospective.TotalSpent.Should().Be(150m);
        retrospective.TotalPlanned.Should().Be(100m);
        retrospective.TopSpenders.Should().ContainSingle(s => s.ParticipantId == ownerParticipant.Id && s.TotalPaid == 150m);
        retrospective.ChecklistTotalCount.Should().Be(1);
        retrospective.ChecklistDoneCount.Should().Be(0);
        retrospective.ItineraryItemsCount.Should().Be(1);
        retrospective.ParticipantsCount.Should().Be(1);
    }

    [Fact]
    public async Task ListarGastos_FiltraPorCategoriaEBuscaEPagina()
    {
        var (owner, _) = await RegisterAndLoginAsync(_factory.CreateClient(), "Dono");

        var createTripResponse = await owner.PostAsJsonAsync("/api/trips", new CreateTripRequest("Viagem com filtros", null, null, null, null, "BRL"));
        var trip = await createTripResponse.Content.ReadFromJsonAsync<TripDto>();

        var participantsResponse = await owner.GetAsync($"/api/trips/{trip!.Id}/participants");
        var ownerParticipant = (await participantsResponse.Content.ReadFromJsonAsync<List<ParticipantDto>>())!.Single();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var (description, category) in new[] { ("Hotel Lisboa", "Hospedagem"), ("Uber aeroporto", "Transporte"), ("Jantar", "Alimentacao") })
        {
            await owner.PostAsJsonAsync($"/api/trips/{trip.Id}/expenses", new CreateExpenseRequest(
                description, 50m, category, ownerParticipant.Id, today, null, null));
        }

        var byCategory = await owner.GetFromJsonAsync<PagedResult<ExpenseDto>>($"/api/trips/{trip.Id}/expenses?category=Transporte");
        byCategory!.TotalCount.Should().Be(1);
        byCategory.Items.Single().Description.Should().Be("Uber aeroporto");

        var bySearch = await owner.GetFromJsonAsync<PagedResult<ExpenseDto>>($"/api/trips/{trip.Id}/expenses?search=hotel");
        bySearch!.TotalCount.Should().Be(1);
        bySearch.Items.Single().Description.Should().Be("Hotel Lisboa");

        var firstPage = await owner.GetFromJsonAsync<PagedResult<ExpenseDto>>($"/api/trips/{trip.Id}/expenses?pageSize=2&page=1");
        firstPage!.TotalCount.Should().Be(3);
        firstPage.Items.Should().HaveCount(2);

        var secondPage = await owner.GetFromJsonAsync<PagedResult<ExpenseDto>>($"/api/trips/{trip.Id}/expenses?pageSize=2&page=2");
        secondPage!.Items.Should().HaveCount(1);
    }
}
