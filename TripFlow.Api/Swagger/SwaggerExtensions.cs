using Microsoft.OpenApi;

namespace TripFlow.Api.Swagger;

public static class SwaggerExtensions
{
    private const string BearerSchemeId = "Bearer";

    public static IServiceCollection AddTripFlowSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "TripFlow API", Version = "v1" });

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
