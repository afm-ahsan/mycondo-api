using AwesomeAssertions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Domain.UnitTests.Features.Identity;

public class RoleTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void BackfillCode_Sets_Code_On_A_Legacy_Role_With_No_Code()
    {
        Role role = Role.CreateCustom(TenantId, "BuildingAdmin", "desc", Now);
        role.Code.Should().BeNull();

        role.BackfillCode("default.building-admin");

        role.Code.Should().Be("default.building-admin");
    }

    [Fact]
    public void BackfillCode_Trims_The_Value()
    {
        Role role = Role.CreateCustom(TenantId, "BuildingAdmin", "desc", Now);

        role.BackfillCode("  default.building-admin  ");

        role.Code.Should().Be("default.building-admin");
    }

    [Fact]
    public void BackfillCode_Throws_If_The_Role_Already_Has_A_Code()
    {
        Role role = Role.CreateCustom(TenantId, "BuildingAdmin", "desc", Now, code: "default.building-admin");

        Action act = () => role.BackfillCode("something.else");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already has Code*");
    }

    [Fact]
    public void BackfillCode_Throws_On_Null_Or_Whitespace()
    {
        Role role = Role.CreateCustom(TenantId, "BuildingAdmin", "desc", Now);

        Action act = () => role.BackfillCode("   ");

        act.Should().Throw<ArgumentException>();
    }
}
