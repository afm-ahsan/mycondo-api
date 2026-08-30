using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Settings;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Platform.PlatformRefreshTokens;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Infrastructure.Identity;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Identity;

/// <summary>
/// ADR-033 Task 13H.1 §6 — proves <see cref="JwtTokenService"/> and <see cref="PlatformJwtTokenService"/>
/// issue access tokens against their own distinct configured audience, so a future regression that
/// accidentally reused the other identity type's audience is caught here directly, without depending on
/// the Docker-only round-trip DB tests (PlatformAuthEndpointsDbTests) to notice.
/// </summary>
public class JwtTokenAudienceIssuanceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "https://api.condobd.com",
        Audience = "https://api.condobd.com",
        PlatformAudience = "https://platform.condobd.com",
        SigningKey = "test-only-signing-key-not-for-any-real-environment-0123456789",
    };

    [Fact]
    public async Task Tenant_Token_Uses_Configured_Tenant_Audience()
    {
        JwtTokenService service = new(
            Options.Create(Settings),
            Substitute.For<IUserContextResolver>(),
            Substitute.For<IRefreshTokenRepository>(),
            Substitute.For<IUserRepository>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IClock>(),
            NullLogger<JwtTokenService>.Instance);

        AuthenticatedUserDto user = new(
            UserId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            TenantName: "Test Tenant",
            Email: "user@example.com",
            FullName: "Test User",
            Roles: [],
            Permissions: [],
            BuildingIds: [],
            BuildingPermissions: [],
            Entitlements: []);

        AuthTokensDto tokens = await service.IssueAsync(user, "127.0.0.1", CancellationToken.None);

        GetAudience(tokens.AccessToken).Should().Be(Settings.Audience);
    }

    [Fact]
    public async Task Platform_Token_Uses_Configured_Platform_Audience()
    {
        PlatformJwtTokenService service = new(
            Options.Create(Settings),
            Substitute.For<IPlatformUserContextResolver>(),
            Substitute.For<IPlatformRefreshTokenRepository>(),
            Substitute.For<IPlatformUserRepository>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IClock>(),
            NullLogger<PlatformJwtTokenService>.Instance);

        PlatformAuthenticatedUserDto user = new(
            PlatformUserId: Guid.NewGuid(),
            Email: "platform@example.com",
            DisplayName: "Platform User",
            Roles: [],
            Permissions: []);

        PlatformAuthTokensDto tokens = await service.IssueAsync(user, "127.0.0.1", CancellationToken.None);

        GetAudience(tokens.AccessToken).Should().Be(Settings.PlatformAudience);
    }

    [Fact]
    public void Tenant_And_Platform_Audiences_Are_Configured_Distinctly()
    {
        Settings.Audience.Should().NotBe(Settings.PlatformAudience);
    }

    private static string GetAudience(string accessToken) =>
        new JsonWebTokenHandler().ReadJsonWebToken(accessToken).Audiences.Single();
}
