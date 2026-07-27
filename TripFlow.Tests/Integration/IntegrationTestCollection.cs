using Xunit;

namespace TripFlow.Tests.Integration;

/// <summary>
/// Cada teste de integracao sobe um host ASP.NET Core inteiro (WebApplicationFactory) - rodar
/// varios em paralelo (padrao do xUnit) sobrecarrega a thread pool e causa falha intermitente
/// no handshake do SignalR (500 no poll do LongPolling). Serializa so essa categoria; os
/// testes de unidade continuam paralelos normalmente.
/// </summary>
[CollectionDefinition("Integration", DisableParallelization = true)]
public class IntegrationTestCollection;
