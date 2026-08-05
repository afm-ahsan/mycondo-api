using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Property;

public class FlatTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();

    [Fact]
    public void Create_Trims_FlatNumber()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "  A-501  ", 5, FlatType.Residential, Now);

        flat.FlatNumber.Should().Be("A-501");
        flat.FloorNumber.Should().Be(5);
        flat.FlatType.Should().Be(FlatType.Residential);
        flat.Version.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_When_FlatNumber_Is_Blank(string flatNumber)
    {
        Action act = () => Flat.Create(TenantId, BuildingId, flatNumber, null, FlatType.Residential, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_Increments_Version()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-501", 5, FlatType.Residential, Now);

        flat.UpdateDetails("A-502", 5, FlatType.Commercial);

        flat.FlatNumber.Should().Be("A-502");
        flat.FlatType.Should().Be(FlatType.Commercial);
        flat.Version.Should().Be(2);
    }

    [Fact]
    public void Deactivate_Sets_DeletedAtUtc()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-501", null, FlatType.Residential, Now);

        flat.Deactivate(Now.AddDays(1), Guid.NewGuid());

        flat.DeletedAtUtc.Should().Be(Now.AddDays(1));
    }
}
