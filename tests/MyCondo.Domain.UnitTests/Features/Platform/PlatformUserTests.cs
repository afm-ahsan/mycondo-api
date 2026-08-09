using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Domain.UnitTests.Features.Platform;

public class PlatformUserTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_Normalizes_Email_And_Trims_DisplayName()
    {
        PlatformUser user = PlatformUser.Create("  SAdmin@MyCondo.com  ", "hash", "  Platform SuperAdmin  ", Now);

        user.Email.Should().Be("sadmin@mycondo.com");
        user.DisplayName.Should().Be("Platform SuperAdmin");
        user.Status.Should().Be(PlatformUserStatus.Active);
        user.Version.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_Email_Is_Blank(string email)
    {
        Action act = () => PlatformUser.Create(email, "hash", "Name", Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PlatformUser_Has_No_TenantId_Field()
    {
        // The core architectural invariant of the whole Phase 1 change: a Platform identity is not,
        // and must never accidentally become, a tenant-scoped row. See mycondo-docs ADR-019.
        typeof(PlatformUser).GetProperties().Select(p => p.Name)
            .Should().NotContain(name =>
                name.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Building", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Organization", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Condominium", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RecordLogin_Sets_LastLoginAtUtc()
    {
        PlatformUser user = PlatformUser.Create("a@b.com", "hash", "Name", Now);

        user.RecordLogin(Now.AddMinutes(1));

        user.LastLoginAtUtc.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void ChangePassword_Bumps_Version_When_Hash_Differs()
    {
        PlatformUser user = PlatformUser.Create("a@b.com", "hash-1", "Name", Now);

        user.ChangePassword("hash-2", Now.AddMinutes(1));

        user.PasswordHash.Should().Be("hash-2");
        user.Version.Should().Be(2);
    }

    [Fact]
    public void ChangePassword_Is_A_NoOp_When_Hash_Is_Unchanged()
    {
        PlatformUser user = PlatformUser.Create("a@b.com", "hash-1", "Name", Now);

        user.ChangePassword("hash-1", Now.AddMinutes(1));

        user.Version.Should().Be(1);
    }
}
