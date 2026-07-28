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
            });

        services.AddAuthorization();

        return services;
    }
}
