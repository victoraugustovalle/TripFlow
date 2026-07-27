using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TripFlow.Application.Abstractions;
using TripFlow.Infrastructure.Data;

namespace TripFlow.Tests.Integration;

/// <summary>
/// Sobe a Api inteira (mesmo pipeline, mesma DI) trocando so o banco (Postgres -> SQLite
/// em memoria, seguindo o mesmo padrao de teste de integracao que uso no RachaContas) e o
/// envio de e-mail (SMTP -> TestEmailSender) - da pra testar o fluxo real de ponta a ponta
/// sem precisar de um Postgres de verdade rodando.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:PrivateKeyPem"] = TestRsaKeys.PrivateKeyPem,
                ["Jwt:PublicKeyPem"] = TestRsaKeys.PublicKeyPem,
                ["Jwt:Issuer"] = "TripFlow.Tests",
                ["Jwt:Audience"] = "TripFlow.Tests.Api"
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddDbContext acumula a configuracao (UseNpgsql da Infrastructure real) via
            // IDbContextOptionsConfiguration<T> - remover so o DbContextOptions<T> final
            // nao basta, os dois providers ficam configurados no mesmo options builder.
            services.RemoveAll<DbContextOptions<TripFlowDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<TripFlowDbContext>));
            services.AddDbContext<TripFlowDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // No ambiente "Testing" o Program.cs pula o Database.Migrate() (as migrations
        // foram geradas pro Postgres - aplicar contra SQLite dispara falso positivo de
        // "pending model changes"). EnsureCreated() so cria o schema a partir do modelo
        // atual, sem depender do historico de migrations.
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TripFlowDbContext>().Database.EnsureCreated();

        return host;
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
