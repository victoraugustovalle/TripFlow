using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OtpNet;
using TripFlow.Api.Controllers;
using TripFlow.Application.Auth.DTOs;
using TripFlow.Application.TwoFactor.DTOs;
using Xunit;

namespace TripFlow.Tests.Integration;

[Collection("Integration")]
public class TwoFactorAuthTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, string Email, string Password)> RegisterAndLoginAsync()
    {
        var client = _factory.CreateClient();
        var email = $"2fa-{Guid.NewGuid():N}@example.com";
        const string password = "SenhaForte@123";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Usuario 2FA", email, password));
        var code = _factory.EmailSender.GetCode(email, "Confirme seu e-mail");
        await client.PostAsJsonAsync("/api/auth/confirm-email", new ConfirmEmailRequest(email, code!));

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Auth!.AccessToken);
        return (client, email, password);
    }

    private async Task<(string Secret, IReadOnlyList<string> RecoveryCodes)> EnableTwoFactorAsync(HttpClient authedClient)
    {
        var setupResponse = await authedClient.PostAsync("/api/auth/2fa/setup", null);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupResult>();

        var code = ComputeTotp(setup!.Secret);
        var enableResponse = await authedClient.PostAsJsonAsync("/api/auth/2fa/enable", new EnableTwoFactorRequest(code));
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var recoveryCodes = await enableResponse.Content.ReadFromJsonAsync<List<string>>();

        return (setup.Secret, recoveryCodes!);
    }

    private static string ComputeTotp(string base32Secret) => new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp();

    [Fact]
    public async Task FluxoCompleto_SetupEnableLoginComDesafioEVerify_FuncionaDePontaAPonta()
    {
        var (client, email, password) = await RegisterAndLoginAsync();
        var (secret, recoveryCodes) = await EnableTwoFactorAsync(client);
        recoveryCodes.Should().HaveCount(8);

        var anonymousClient = _factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult!.RequiresTwoFactor.Should().BeTrue();
        loginResult.Auth.Should().BeNull();
        loginResult.TwoFactorChallengeToken.Should().NotBeNullOrEmpty();

        var verifyResponse = await anonymousClient.PostAsJsonAsync("/api/auth/2fa/verify", new VerifyTwoFactorRequest(
            email, loginResult.TwoFactorChallengeToken!, ComputeTotp(secret), null));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await verifyResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.User.TwoFactorEnabled.Should().BeTrue();

        using var authedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/trips");
        authedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var tripsResponse = await anonymousClient.SendAsync(authedRequest);
        tripsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Verify_CodigoTotpErrado_RetornaUnauthorized()
    {
        var (client, email, password) = await RegisterAndLoginAsync();
        await EnableTwoFactorAsync(client);

        var anonymousClient = _factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var verifyResponse = await anonymousClient.PostAsJsonAsync("/api/auth/2fa/verify", new VerifyTwoFactorRequest(
            email, loginResult!.TwoFactorChallengeToken!, "000000", null));

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Verify_ComCodigoDeRecuperacao_FuncionaUmaVezSoDepoisFalha()
    {
        var (client, email, password) = await RegisterAndLoginAsync();
        var (_, recoveryCodes) = await EnableTwoFactorAsync(client);
        var recoveryCode = recoveryCodes[0];

        var anonymousClient = _factory.CreateClient();

        var firstLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var firstLoginResult = await firstLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var firstVerifyResponse = await anonymousClient.PostAsJsonAsync("/api/auth/2fa/verify", new VerifyTwoFactorRequest(
            email, firstLoginResult!.TwoFactorChallengeToken!, null, recoveryCode));
        firstVerifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Mesmo codigo de recuperacao numa segunda tentativa (novo desafio de login) - ja foi consumido.
        var secondLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var secondLoginResult = await secondLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var secondVerifyResponse = await anonymousClient.PostAsJsonAsync("/api/auth/2fa/verify", new VerifyTwoFactorRequest(
            email, secondLoginResult!.TwoFactorChallengeToken!, null, recoveryCode));

        secondVerifyResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disable_ComSenhaECodigoValidos_DesligaEProximoLoginNaoPedeMaisDesafio()
    {
        var (client, email, password) = await RegisterAndLoginAsync();
        var (secret, _) = await EnableTwoFactorAsync(client);

        var disableResponse = await client.PostAsJsonAsync("/api/auth/2fa/disable", new DisableTwoFactorRequest(password, ComputeTotp(secret)));
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginResult!.RequiresTwoFactor.Should().BeFalse();
        loginResult.Auth.Should().NotBeNull();
    }

    [Fact]
    public async Task Disable_ComSenhaErrada_RetornaUnauthorizedENaoDesliga()
    {
        var (client, email, password) = await RegisterAndLoginAsync();
        var (secret, _) = await EnableTwoFactorAsync(client);

        var disableResponse = await client.PostAsJsonAsync("/api/auth/2fa/disable", new DisableTwoFactorRequest("SenhaErrada@123", ComputeTotp(secret)));
        disableResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult!.RequiresTwoFactor.Should().BeTrue();
    }

    [Fact]
    public async Task Setup_SemAutenticacao_RetornaUnauthorized()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsync("/api/auth/2fa/setup", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
