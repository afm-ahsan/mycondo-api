using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.PlatformRefreshTokens;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Domain.UnitTests.Features.Platform;

public class PlatformRefreshTokenTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly PlatformUserId UserId = PlatformUserId.New();

    [Fact]
    public void Issue_Is_Active_Before_Expiry()
    {
        PlatformRefreshToken token = PlatformRefreshToken.Issue(
            UserId, "hash", Now.AddDays(7), Now, "127.0.0.1");

        token.IsActive(Now).Should().BeTrue();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_True_After_ExpiresAtUtc()
    {
        PlatformRefreshToken token = PlatformRefreshToken.Issue(
            UserId, "hash", Now.AddDays(7), Now, "127.0.0.1");

        token.IsExpired(Now.AddDays(8)).Should().BeTrue();
        token.IsActive(Now.AddDays(8)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_Sets_RevokedAtUtc_And_Deactivates()
    {
        PlatformRefreshToken token = PlatformRefreshToken.Issue(
            UserId, "hash", Now.AddDays(7), Now, "127.0.0.1");

        token.Revoke(Now.AddHours(1), "127.0.0.1");

        token.IsRevoked.Should().BeTrue();
        token.IsActive(Now.AddHours(2)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_Is_Idempotent()
    {
        PlatformRefreshToken token = PlatformRefreshToken.Issue(
            UserId, "hash", Now.AddDays(7), Now, "127.0.0.1");

        token.Revoke(Now.AddHours(1), "127.0.0.1");
        DateTimeOffset? firstRevokedAt = token.RevokedAtUtc;
        token.Revoke(Now.AddHours(2), "10.0.0.1");

        token.RevokedAtUtc.Should().Be(firstRevokedAt);
    }

    [Fact]
    public void PlatformRefreshToken_Is_Keyed_On_PlatformUserId_Not_TenantId()
    {
        typeof(PlatformRefreshToken).GetProperties().Select(p => p.Name)
            .Should().NotContain(name => name.Contains("Tenant", StringComparison.OrdinalIgnoreCase));
    }
}
