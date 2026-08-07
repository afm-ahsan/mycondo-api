using AwesomeAssertions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.BlackoutDates.Exceptions;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Domain.UnitTests.Features.Amenities.BlackoutDates;

public class BlackoutDateTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FacilityId FacilityId = FacilityId.New();

    [Fact]
    public void Create_Starts_Active()
    {
        BlackoutDate blackout = BlackoutDate.Create(
            TenantId, FacilityId, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), "Maintenance closure", Now);

        blackout.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_When_DateTo_Precedes_DateFrom()
    {
        Action act = () => BlackoutDate.Create(
            TenantId, FacilityId, new DateOnly(2026, 12, 26), new DateOnly(2026, 12, 24), "Bad range", Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Covers_ReturnsTrue_When_Active_And_Within_Range()
    {
        BlackoutDate blackout = BlackoutDate.Create(
            TenantId, FacilityId, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), "Maintenance closure", Now);

        blackout.Covers(new DateOnly(2026, 12, 25)).Should().BeTrue();
        blackout.Covers(new DateOnly(2026, 12, 27)).Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Then_Covers_ReturnsFalse()
    {
        BlackoutDate blackout = BlackoutDate.Create(
            TenantId, FacilityId, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), "Maintenance closure", Now);

        blackout.Deactivate();

        blackout.IsActive.Should().BeFalse();
        blackout.Covers(new DateOnly(2026, 12, 25)).Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Throws_When_Already_Inactive()
    {
        BlackoutDate blackout = BlackoutDate.Create(
            TenantId, FacilityId, new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), "Maintenance closure", Now);
        blackout.Deactivate();

        Action act = () => blackout.Deactivate();

        act.Should().Throw<BlackoutDateAlreadyInactiveException>();
    }
}
