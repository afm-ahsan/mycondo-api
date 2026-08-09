using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyCondo.Application.Common.Settings;

namespace MyCondo.Api.Authentication;

public static class JwtBearerSetup
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The tenant scheme stays the DEFAULT (JwtBearerDefaults.AuthenticationScheme, "Bearer") —
        // every existing .RequireAuthorization()/.RequirePermission() call across the whole API
        // keeps working completely unchanged. Its ValidAudience (settings.Audience) already rejects
        // a Platform-audience token outright, so tenant endpoints need zero code changes to reject
        // Platform tokens — see mycondo-docs ADR-019.
        //
        // The Platform scheme is registered alongside it, under its own name, with its own
        // ValidAudience (settings.PlatformAudience) — but is never made the default. Only endpoints
        // that explicitly opt in via RequirePlatformPermission ever authenticate against it.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                JwtSettings settings = services.BuildServiceProvider()
                    .GetRequiredService<IOptions<JwtSettings>>().Value;

                options.RequireHttpsMetadata = false; // toggled by env in Program.cs
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            })
            .AddJwtBearer(PlatformAuthenticationDefaults.SchemeName, options =>
            {
                JwtSettings settings = services.BuildServiceProvider()
                    .GetRequiredService<IOptions<JwtSettings>>().Value;

                options.RequireHttpsMetadata = false; // toggled by env in Program.cs
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.PlatformAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PlatformAuthenticationDefaults.AuthorizationPolicyName, policy =>
                policy
                    .AddAuthenticationSchemes(PlatformAuthenticationDefaults.SchemeName)
                    .RequireAuthenticatedUser());
        });

        return services;
    }
}
