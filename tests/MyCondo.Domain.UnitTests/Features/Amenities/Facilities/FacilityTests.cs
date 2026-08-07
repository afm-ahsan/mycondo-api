using AwesomeAssertions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.UnitTests.Features.Amenities.Facilities;

public class FacilityTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();

    private static Facility CreateFacility(int capacity = 100, decimal? bookingCharge = 500m, decimal? deposit = 2000m) =>
        Facility.Create(
            TenantId, BuildingId, "Community Hall", FacilityType.CommunityHall, capacity, null, null, true,
            bookingCharge, deposit, 24, 50m, null, null, false, false, Now);

    [Fact]
    public void Create_Starts_Active_With_Version_One()
    {
        Facility facility = CreateFacility();

        facility.IsActive.Should().BeTrue();
        facility.Version.Should().Be(1);
        facility.FacilityType.Should().Be(FacilityType.CommunityHall);
    }

    [Fact]
    public void Create_Throws_When_Capacity_Not_Positive()
    {
        Action act = () => Facility.Create(
            TenantId, BuildingId, "Hall", FacilityType.CommunityHall, 0, null, null, false, null, null, 24, 0, null,
            null, false, false, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Throws_When_CancellationDeductionPercentage_Out_Of_Range()
    {
        Action act = () => Facility.Create(
            TenantId, BuildingId, "Hall", FacilityType.CommunityHall, 10, null, null, false, null, null, 24, 150m,
            null, null, false, false, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateConfiguration_Updates_Fields_And_Bumps_Version()
    {
        Facility facility = CreateFacility();

        facility.UpdateConfiguration("Renamed Hall", 150, null, null, false, 600m, 2500m, 48, 25m, null, null, false, true);

        facility.Name.Should().Be("Renamed Hall");
        facility.Capacity.Should().Be(150);
        facility.BookingChargeAmount.Should().Be(600m);
        facility.BlocksEntryIfAccountOverdue.Should().BeTrue();
        facility.Version.Should().Be(2);
    }

    [Fact]
    public void Deactivate_Then_Reactivate_Toggles_IsActive()
    {
        Facility facility = CreateFacility();

        facility.Deactivate();
        facility.IsActive.Should().BeFalse();

        facility.Reactivate();
        facility.IsActive.Should().BeTrue();
        facility.Version.Should().Be(3);
    }
}
