using AwesomeAssertions;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Domain.UnitTests.Features.Security.ParcelCustodyEvents;

public class ParcelCustodyEventTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly ParcelId ParcelId = ParcelId.New();

    [Fact]
    public void Record_Trims_Notes()
    {
        Guid performedBy = Guid.NewGuid();

        ParcelCustodyEvent custodyEvent = ParcelCustodyEvent.Record(
            TenantId, ParcelId, ParcelStatus.Received, performedBy, "  Parcel received  ", Now);

        custodyEvent.ToStatus.Should().Be(ParcelStatus.Received);
        custodyEvent.PerformedBy.Should().Be(performedBy);
        custodyEvent.Notes.Should().Be("Parcel received");
        custodyEvent.OccurredAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Record_Allows_Null_Notes_And_PerformedBy()
    {
        ParcelCustodyEvent custodyEvent = ParcelCustodyEvent.Record(
            TenantId, ParcelId, ParcelStatus.Collected, null, null, Now);

        custodyEvent.PerformedBy.Should().BeNull();
        custodyEvent.Notes.Should().BeNull();
    }
}
