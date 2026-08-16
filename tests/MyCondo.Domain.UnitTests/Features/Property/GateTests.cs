using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Domain.UnitTests.Features.Property;

public class GateTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();

    [Fact]
    public void Create_Trims_Name_And_Uppercases_Code()
    {
        Gate gate = Gate.Create(TenantId, BuildingId, "  Main Gate  ", " main ", " front entrance ", true, true, 1, Now);

        gate.Name.Should().Be("Main Gate");
        gate.Code.Should().Be("MAIN");
        gate.Description.Should().Be("front entrance");
        gate.BuildingId.Should().Be(BuildingId);
        gate.IsActive.Should().BeTrue();
        gate.IsEntryAllowed.Should().BeTrue();
        gate.IsExitAllowed.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_Name_Is_Blank(string name)
    {
        Action act = () => Gate.Create(TenantId, BuildingId, name, "MAIN", null, true, true, 0, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_Code_Is_Blank(string code)
    {
        Action act = () => Gate.Create(TenantId, BuildingId, "Main Gate", code, null, true, true, 0, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Changes_Fields_And_Bumps_Version()
    {
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", "MAIN", null, true, true, 0, Now);

        gate.Update("Basement Ramp", "basement", "Vehicle only", false, true, 2, Now);

        gate.Name.Should().Be("Basement Ramp");
        gate.Code.Should().Be("BASEMENT");
        gate.Description.Should().Be("Vehicle only");
        gate.IsEntryAllowed.Should().BeFalse();
        gate.IsExitAllowed.Should().BeTrue();
        gate.DisplayOrder.Should().Be(2);
        gate.Version.Should().Be(2);
    }

    [Fact]
    public void Deactivate_Then_Activate_Round_Trips_And_Bumps_Version_Each_Time()
    {
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", "MAIN", null, true, true, 0, Now);

        gate.Deactivate(Now);
        gate.IsActive.Should().BeFalse();
        gate.Version.Should().Be(2);

        gate.Activate(Now);
        gate.IsActive.Should().BeTrue();
        gate.Version.Should().Be(3);
    }

    [Fact]
    public void Deactivate_Is_Idempotent()
    {
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", "MAIN", null, true, true, 0, Now);
        gate.Deactivate(Now);
        int versionAfterFirstDeactivate = gate.Version;

        gate.Deactivate(Now);

        gate.Version.Should().Be(versionAfterFirstDeactivate);
    }
}
