using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using TripFlow.Api.Authentication;
using TripFlow.Api.Authorization;
using TripFlow.Api.BackgroundJobs;
using TripFlow.Api.RateLimiting;
using TripFlow.Api.RealTime;
using TripFlow.Api.Swagger;
using TripFlow.Api.Validation;
using TripFlow.Application;
using TripFlow.Infrastructure;
using TripFlow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTripFlowSwagger();
builder.Services.AddProblemDetails();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddTripFlowAuthentication();
builder.Services.AddTripAuthorization();
builder.Services.AddTripFlowRateLimiting();
builder.Services.AddTripFlowRealTime();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddHealthChecks();

// So roda contra o banco real - os testes de integracao usam WebApplicationFactory com um
// DbContext isolado por teste, e um job em background com seu proprio escopo/conexao entraria
// em conflito com isso sem trazer nenhum valor pro teste em si.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<ProactiveNotificationsHostedService>();

// O IP do cliente (usado pelo rate limiter e pelo CreatedByIp do refresh token) so vem
// certo se a gente confiar no X-Forwarded-For do proxy na frente (Fly.io, etc.) - sem
// isso, toda requisicao chega com o IP interno do proxy e cai no mesmo balde de limite.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Nos testes de integracao (WebApplicationFactory com ambiente "Testing") o banco e
// SQLite so pra teste, e a factory cuida do proprio schema - rodar migration gerada
// pro Postgres contra outro provider dispara falso positivo de "model changes".
if (!app.Environment.IsEnvironment("Testing"))
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<TripFlowDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "Ocorreu um erro interno." });
    }));
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapTripFlowRealTime();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
