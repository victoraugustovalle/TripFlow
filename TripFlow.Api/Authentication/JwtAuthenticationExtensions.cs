using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TripFlow.Application.Abstractions;
using TripFlow.Application.Common;

namespace TripFlow.Api.Authentication;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddTripFlowAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };

                // O access token e curto e assinado (RS256) - a unica forma de invalida-lo
                // antes do vencimento natural (logout, reuso de refresh token detectado) e
                // essa checagem de denylist por jti a cada request autenticado.
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (jti is null)
                        {
                            context.Fail("Token sem jti.");
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<IAppDbContext>();
                        var isRevoked = await db.RevokedAccessTokens.AsNoTracking().AnyAsync(t => t.Jti == jti);
                        if (isRevoked)
                            context.Fail("Token revogado.");
                    }
                };
            });

        // As chaves RSA (config) so ficam disponiveis depois do DI montado - PostConfigure
        // roda no primeiro uso do JwtBearerHandler, ja com IOptions<JwtOptions> resolvido.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                var jwt = jwtOptionsAccessor.Value;

                var rsa = RSA.Create();
                rsa.ImportFromPem(jwt.PublicKeyPem);

                bearerOptions.TokenValidationParameters.ValidIssuer = jwt.Issuer;
                bearerOptions.TokenValidationParameters.ValidAudience = jwt.Audience;
                bearerOptions.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(rsa);
            });

        return services;
    }
}
