using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Property.Gates;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.AccessSessions.Exceptions;
using MyCondo.Domain.Features.Security.Guests;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Domain.UnitTests.Features.Security.AccessSessions;

public class AccessSessionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GateId GateId = GateId.New();

    [Fact]
    public void CheckInGuest_Starts_CheckedIn_With_Approved_Status()
    {
        AccessSession session = AccessSession.CheckInGuest(
            TenantId, GuestProfileId.New(), FlatId.New(), "Family visit", GateId, Guid.NewGuid(),
            "QR-001", "Remarks", null, Now);

        session.AccessCategory.Should().Be(AccessCategory.Guest);
        session.Status.Should().Be(AccessSessionStatus.CheckedIn);
        session.ApprovalStatus.Should().Be(AccessApprovalStatus.Approved);
        session.EntryAtUtc.Should().Be(Now);
        session.ExitAtUtc.Should().BeNull();
        session.VehicleId.Should().BeNull();
    }

    [Fact]
    public void CheckInVehicle_Starts_CheckedIn()
    {
        AccessSession session = AccessSession.CheckInVehicle(
            TenantId, VehicleId.New(), null, GateId, Guid.NewGuid(), null, null, Now);

        session.AccessCategory.Should().Be(AccessCategory.Vehicle);
        session.Status.Should().Be(AccessSessionStatus.CheckedIn);
        session.GuestProfileId.Should().BeNull();
    }

    [Fact]
    public void CheckOut_Transitions_To_CheckedOut()
    {
        AccessSession session = AccessSession.CheckInGuest(
            TenantId, GuestProfileId.New(), FlatId.New(), null, GateId, Guid.NewGuid(), null, null, null, Now);
        GateId exitGate = GateId.New();
        Guid checkedOutBy = Guid.NewGuid();

        session.CheckOut(exitGate, checkedOutBy, Now.AddHours(1));

        session.Status.Should().Be(AccessSessionStatus.CheckedOut);
        session.ExitGateId.Should().Be(exitGate);
        session.ExitAtUtc.Should().Be(Now.AddHours(1));
        session.CheckedOutBy.Should().Be(checkedOutBy);
    }

    [Fact]
    public void CheckOut_Throws_When_Already_CheckedOut()
    {
        AccessSession session = AccessSession.CheckInGuest(
            TenantId, GuestProfileId.New(), FlatId.New(), null, GateId, Guid.NewGuid(), null, null, null, Now);
        session.CheckOut(GateId.New(), Guid.NewGuid(), Now.AddHours(1));

        Action act = () => session.CheckOut(GateId.New(), Guid.NewGuid(), Now.AddHours(2));

        act.Should().Throw<AccessSessionAlreadyClosedException>();
    }
}
