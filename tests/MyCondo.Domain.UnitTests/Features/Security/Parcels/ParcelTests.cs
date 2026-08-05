using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Parcels;
using MyCondo.Domain.Features.Security.Parcels.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Security.Parcels;

public class ParcelTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();

    private static Parcel ReceiveParcel() =>
        Parcel.Receive(TenantId, "REF-1", "Pathao", "TRK-1", "Amazon", FlatId, null, ParcelType.Package, 1, Guid.NewGuid(), "Shelf A1", Now);

    [Fact]
    public void Receive_Starts_Received_With_NotSent_Notification()
    {
        Parcel parcel = ReceiveParcel();

        parcel.Status.Should().Be(ParcelStatus.Received);
        parcel.NotificationStatus.Should().Be(ParcelNotificationStatus.NotSent);
        parcel.Version.Should().Be(1);
    }

    [Fact]
    public void Receive_Throws_When_PackageCount_Not_Positive()
    {
        Action act = () => Parcel.Receive(
            TenantId, null, null, null, null, FlatId, null, ParcelType.Package, 0, null, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NotifyResident_Transitions_To_AwaitingCollection()
    {
        Parcel parcel = ReceiveParcel();

        parcel.NotifyResident();

        parcel.Status.Should().Be(ParcelStatus.AwaitingCollection);
        parcel.NotificationStatus.Should().Be(ParcelNotificationStatus.Sent);
    }

    [Fact]
    public void NotifyResident_Throws_When_Not_Received()
    {
        Parcel parcel = ReceiveParcel();
        parcel.NotifyResident();

        Action act = () => parcel.NotifyResident();

        act.Should().Throw<ParcelInvalidStatusTransitionException>();
    }

    [Fact]
    public void Collect_Sets_Collected_Fields()
    {
        Parcel parcel = ReceiveParcel();
        parcel.NotifyResident();
        Guid collectedBy = Guid.NewGuid();

        parcel.Collect(collectedBy, "Jane Resident", "OTP-1234", Now.AddHours(2));

        parcel.Status.Should().Be(ParcelStatus.Collected);
        parcel.CollectedBy.Should().Be(collectedBy);
        parcel.CollectorName.Should().Be("Jane Resident");
        parcel.CollectionAcknowledgement.Should().Be("OTP-1234");
        parcel.CollectedAtUtc.Should().Be(Now.AddHours(2));
    }

    [Fact]
    public void Collect_Throws_When_Already_Collected()
    {
        Parcel parcel = ReceiveParcel();
        parcel.Collect(Guid.NewGuid(), "Jane Resident", null, Now);

        Action act = () => parcel.Collect(Guid.NewGuid(), "Someone Else", null, Now.AddHours(1));

        act.Should().Throw<ParcelInvalidStatusTransitionException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Collect_Throws_When_CollectorName_Is_Blank(string collectorName)
    {
        Parcel parcel = ReceiveParcel();

        Action act = () => parcel.Collect(Guid.NewGuid(), collectorName, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkDamaged_Sets_Status_And_Note()
    {
        Parcel parcel = ReceiveParcel();

        parcel.MarkDamaged("Box crushed in transit");

        parcel.Status.Should().Be(ParcelStatus.Damaged);
        parcel.DamageNote.Should().Be("Box crushed in transit");
    }

    [Theory]
    [InlineData(ParcelStatus.Returned)]
    [InlineData(ParcelStatus.Rejected)]
    [InlineData(ParcelStatus.LostOrEscalated)]
    public void Close_Sets_Status_And_Reason_For_Valid_Outcomes(ParcelStatus outcome)
    {
        Parcel parcel = ReceiveParcel();

        parcel.Close(outcome, "Reason text");

        parcel.Status.Should().Be(outcome);
        parcel.CloseReason.Should().Be("Reason text");
    }

    [Fact]
    public void Close_Throws_When_Outcome_Is_Not_A_Valid_Close_Status()
    {
        Parcel parcel = ReceiveParcel();

        Action act = () => parcel.Close(ParcelStatus.Collected, "Reason");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Close_Throws_When_Already_Terminal()
    {
        Parcel parcel = ReceiveParcel();
        parcel.Close(ParcelStatus.Returned, "Wrong address");

        Action act = () => parcel.Close(ParcelStatus.Rejected, "Second attempt");

        act.Should().Throw<ParcelInvalidStatusTransitionException>();
    }
}
