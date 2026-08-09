using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Domain.UnitTests.Features.Platform;

public class PlatformRoleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateSystem_Sets_IsSystem_True()
    {
        PlatformRole role = PlatformRole.CreateSystem(PlatformRoleId.New(), "SuperAdmin", "desc", Now);

        role.IsSystem.Should().BeTrue();
        role.Name.Should().Be("SuperAdmin");
    }

    [Fact]
    public void PlatformRole_Has_No_TenantId_Or_ScopeType_Field()
    {
        // A PlatformRole's mere existence in this table already means "Platform scope" — there is
        // nothing else to discriminate, unlike the tenant Role's TenantId. See mycondo-docs ADR-019.
        typeof(PlatformRole).GetProperties().Select(p => p.Name)
            .Should().NotContain(name =>
                name.Contains("Tenant", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Scope", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Building", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateSystem_Throws_When_Name_Is_Blank(string name)
    {
        Action act = () => PlatformRole.CreateSystem(PlatformRoleId.New(), name, "desc", Now);

        act.Should().Throw<ArgumentException>();
    }
}
