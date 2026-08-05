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
    public void Create_Trims_Name()
    {
        Gate gate = Gate.Create(TenantId, BuildingId, "  Main Gate  ", Now);

        gate.Name.Should().Be("Main Gate");
        gate.BuildingId.Should().Be(BuildingId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_Name_Is_Blank(string name)
    {
        Action act = () => Gate.Create(TenantId, BuildingId, name, Now);

        act.Should().Throw<ArgumentException>();
    }
}
