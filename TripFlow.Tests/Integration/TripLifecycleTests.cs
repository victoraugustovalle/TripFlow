using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TripFlow.Api.Controllers;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.Expenses.DTOs;
using TripFlow.Application.Participants.DTOs;
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
            "Hospedagem", 200m, "Hospedagem", ownerParticipant.Id, DateOnly.FromDateTime(DateTime.UtcNow), null));
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
}
