using AwesomeAssertions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Amenities.PoolSessions.Exceptions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Amenities.PoolSessions;

public class PoolSessionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FacilityId FacilityId = FacilityId.New();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void CheckIn_Starts_CheckedIn()
    {
        PoolSession session = PoolSession.CheckIn(
            TenantId, FacilityId, FlatId, PoolPersonType.Resident, PoolAgeCategory.Adult, null, null, Now,
            Guid.NewGuid(), null, Now);

        session.Status.Should().Be(PoolSessionStatus.CheckedIn);
        session.ExitAtUtc.Should().BeNull();
        session.Version.Should().Be(1);
    }

    [Fact]
    public void CheckIn_Throws_When_GuestFeeAmount_Negative()
    {
        Action act = () => PoolSession.CheckIn(
            TenantId, FacilityId, FlatId, PoolPersonType.Guest, PoolAgeCategory.Adult, null, -10m, null,
            Guid.NewGuid(), null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CheckOut_Closes_The_Session()
    {
        PoolSession session = PoolSession.CheckIn(
            TenantId, FacilityId, FlatId, PoolPersonType.Resident, PoolAgeCategory.Adult, null, null, Now,
            Guid.NewGuid(), null, Now);

        session.CheckOut(Guid.NewGuid(), Now.AddHours(1));

        session.Status.Should().Be(PoolSessionStatus.CheckedOut);
        session.ExitAtUtc.Should().Be(Now.AddHours(1));
        session.Version.Should().Be(2);
    }

    [Fact]
    public void CheckOut_Throws_When_Already_Closed()
    {
        PoolSession session = PoolSession.CheckIn(
            TenantId, FacilityId, FlatId, PoolPersonType.Resident, PoolAgeCategory.Adult, null, null, Now,
            Guid.NewGuid(), null, Now);
        session.CheckOut(Guid.NewGuid(), Now);

        Action act = () => session.CheckOut(Guid.NewGuid(), Now);

        act.Should().Throw<PoolSessionAlreadyClosedException>();
    }

    [Fact]
    public void CheckIn_Child_Records_AccompaniedBySessionId()
    {
        PoolSessionId adultSessionId = PoolSessionId.New();

        PoolSession child = PoolSession.CheckIn(
            TenantId, FacilityId, FlatId, PoolPersonType.Resident, PoolAgeCategory.Child, adultSessionId, null, null,
            Guid.NewGuid(), null, Now);

        child.AccompaniedBySessionId.Should().Be(adultSessionId);
    }
}
