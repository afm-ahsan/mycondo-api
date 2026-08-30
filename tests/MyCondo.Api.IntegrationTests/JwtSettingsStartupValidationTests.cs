using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyCondo.Application.Common.Settings;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// ADR-033 Task 13H.1 §4 — proves the app fails fast at startup when Jwt:Audience and
/// Jwt:PlatformAudience are configured identically, instead of silently collapsing the Platform/tenant
/// scheme boundary the way the previously-committed config did. Builds its own
/// <see cref="WebApplicationFactory{TEntryPoint}"/> (not the shared <see cref="MyCondoWebApplicationFactory"/>)
/// since it needs a config override none of the other tests want.
/// </summary>
public class JwtSettingsStartupValidationTests
{
    [Fact]
    public void Host_Fails_Fast_When_Audience_And_PlatformAudience_Are_Equal()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SigningKey"] = "test-only-signing-key-not-for-any-real-environment",
                        ["Jwt:PlatformAudience"] = "https://api.condobd.com", // deliberately == Jwt:Audience
                    });
                });
            });

        Action act = () => factory.Services.GetRequiredService<IOptions<JwtSettings>>().Value.GetType();

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:Audience and Jwt:PlatformAudience must be distinct*");
    }
}
