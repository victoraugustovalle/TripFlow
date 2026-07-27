using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TripFlow.Api.Controllers;
using TripFlow.Application.Auth.DTOs;
using Xunit;

namespace TripFlow.Tests.Integration;

// Uma factory nova por teste (em vez de IClassFixture compartilhada) - o rate limiter
// de "auth" e apertado de proposito (5/min), entao dividir estado entre testes faria
// um teste derrubar o outro com 429 sem ter nada a ver com o que esta sendo testado.
[Collection("Integration")]
public class AuthFlowTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthFlowTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FluxoCompleto_RegistroConfirmacaoLoginRefreshLogout_FuncionaDePontaAPonta()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "SenhaForte@123";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Maria Teste", email, password));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        registeredUser!.EmailConfirmed.Should().BeFalse();

        var code = _factory.EmailSender.GetCode(email, "Confirme seu e-mail");
        code.Should().NotBeNullOrEmpty();

        var confirmResponse = await _client.PostAsJsonAsync("/api/auth/confirm-email", new ConfirmEmailRequest(email, code!));
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult!.RequiresTwoFactor.Should().BeFalse();
        var auth = loginResult.Auth;
        auth!.AccessToken.Should().NotBeNullOrEmpty();

        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var refreshCookie = cookies!.First(c => c.StartsWith("refreshToken="));

        using var authedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/trips");
        authedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var tripsResponse = await _client.SendAsync(authedRequest);
        tripsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie.Split(';')[0]);
        var refreshResponse = await _client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        refreshed!.AccessToken.Should().NotBe(auth.AccessToken);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        var logoutResponse = await _client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var afterLogoutRequest = new HttpRequestMessage(HttpMethod.Get, "/api/trips");
        afterLogoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        var afterLogoutResponse = await _client.SendAsync(afterLogoutRequest);
        afterLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_SenhaErrada_RetornaUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Joao Teste", email, "SenhaForte@123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "SenhaErrada@123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmailInexistente_RetornaUnauthorizedIgualAoDeSenhaErrada()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ninguem-cadastrado@example.com", "QualquerCoisa@123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
