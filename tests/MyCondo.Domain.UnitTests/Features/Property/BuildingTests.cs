using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.UnitTests.Features.Property;

public class BuildingTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Trims_Name_Address_And_Uppercases_Code()
    {
        Building building = Building.Create(TenantId, "  ARP Tower  ", "  arp1  ", "  123 Gulshan Ave  ", Now);

        building.Name.Should().Be("ARP Tower");
        building.Code.Should().Be("ARP1");
        building.Address.Should().Be("123 Gulshan Ave");
        building.Version.Should().Be(1);
    }

    [Fact]
    public void Create_Allows_Null_Address()
    {
        Building building = Building.Create(TenantId, "ARP Tower", "ARP1", null, Now);

        building.Address.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_Name_Is_Blank(string name)
    {
        Action act = () => Building.Create(TenantId, name, "ARP1", null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_Code_Is_Blank(string code)
    {
        Action act = () => Building.Create(TenantId, "ARP Tower", code, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_When_TenantId_Is_Empty()
    {
        Action act = () => Building.Create(Guid.Empty, "ARP Tower", "ARP1", null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_Increments_Version_And_Uppercases_Code()
    {
        Building building = Building.Create(TenantId, "ARP Tower", "ARP1", null, Now);

        building.UpdateDetails("ARP Tower B", "arp2", "New Address");

        building.Name.Should().Be("ARP Tower B");
        building.Code.Should().Be("ARP2");
        building.Address.Should().Be("New Address");
        building.Version.Should().Be(2);
    }

    [Fact]
    public void Deactivate_Sets_DeletedAtUtc_And_DeletedBy()
    {
        Building building = Building.Create(TenantId, "ARP Tower", "ARP1", null, Now);
        Guid deactivatedBy = Guid.NewGuid();

        building.Deactivate(Now.AddDays(1), deactivatedBy);

        building.DeletedAtUtc.Should().Be(Now.AddDays(1));
        building.DeletedBy.Should().Be(deactivatedBy);
    }

    [Fact]
    public void Deactivate_Is_A_NoOp_When_Already_Deactivated()
    {
        Building building = Building.Create(TenantId, "ARP Tower", "ARP1", null, Now);
        building.Deactivate(Now, Guid.NewGuid());
        Guid secondDeactivator = Guid.NewGuid();

        building.Deactivate(Now.AddDays(1), secondDeactivator);

        building.DeletedAtUtc.Should().Be(Now);
        building.DeletedBy.Should().NotBe(secondDeactivator);
    }
}
