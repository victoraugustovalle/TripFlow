using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Formatting.Compact;
using TripFlow.Api.Authentication;
using TripFlow.Api.Authorization;
using TripFlow.Api.RateLimiting;
using TripFlow.Api.Swagger;
using TripFlow.Application;
using TripFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTripFlowSwagger();
builder.Services.AddProblemDetails();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddTripFlowAuthentication();
builder.Services.AddTripAuthorization();
builder.Services.AddTripFlowRateLimiting();

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
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
