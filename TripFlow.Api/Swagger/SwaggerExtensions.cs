using System.Reflection;
using Microsoft.OpenApi;

namespace TripFlow.Api.Swagger;

public static class SwaggerExtensions
{
    private const string BearerSchemeId = "Bearer";

    public static IServiceCollection AddTripFlowSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TripFlow API",
                Version = "v1",
                Description = "API pra organizar viagem em grupo (participantes, gastos divididos, checklist, orcamento).\n\n" +
                    "**Autenticacao**: faça login em `/api/auth/login` (ou `/api/auth/register` + `/api/auth/confirm-email` antes, " +
                    "se for conta nova). A resposta traz o `accessToken` no corpo - clique em **Authorize** aqui em cima e cole " +
                    "só o token (sem o prefixo `Bearer`). O refresh token vai num cookie `HttpOnly` à parte; o Swagger não " +
                    "mostra isso, mas o navegador guarda sozinho, então `/api/auth/refresh` funciona direto por aqui também.\n\n" +
                    "**Papel por viagem**: a maioria dos endpoints de `/api/trips/{tripId}/...` exige que você seja " +
                    "participante aceito daquela viagem, com o papel mínimo indicado em cada endpoint (Viewer < Editor < Owner)."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Cole so o access token (sem o prefixo 'Bearer ')."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(BearerSchemeId, document, null), new List<string>() }
            });
        });

        return services;
    }
}
