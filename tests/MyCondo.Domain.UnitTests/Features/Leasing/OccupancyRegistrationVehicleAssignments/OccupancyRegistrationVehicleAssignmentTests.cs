using AwesomeAssertions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Domain.UnitTests.Features.Leasing.OccupancyRegistrationVehicleAssignments;

public class OccupancyRegistrationVehicleAssignmentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly OccupancyRegistrationId RegistrationId = OccupancyRegistrationId.New();
    private static readonly VehicleId VehicleId = VehicleId.New();

    [Fact]
    public void Assign_Starts_Active()
    {
        OccupancyRegistrationVehicleAssignment assignment =
            OccupancyRegistrationVehicleAssignment.Assign(TenantId, RegistrationId, VehicleId, Now);

        assignment.IsActive.Should().BeTrue();
        assignment.AssignedAtUtc.Should().Be(Now);
        assignment.EndedAtUtc.Should().BeNull();
    }

    [Fact]
    public void End_Sets_Inactive_And_EndedAtUtc()
    {
        OccupancyRegistrationVehicleAssignment assignment =
            OccupancyRegistrationVehicleAssignment.Assign(TenantId, RegistrationId, VehicleId, Now);
        DateTimeOffset endedAt = Now.AddDays(1);

        assignment.End(endedAt);

        assignment.IsActive.Should().BeFalse();
        assignment.EndedAtUtc.Should().Be(endedAt);
    }

    [Fact]
    public void Assign_Throws_When_TenantId_Empty()
    {
        Action act = () => OccupancyRegistrationVehicleAssignment.Assign(Guid.Empty, RegistrationId, VehicleId, Now);

        act.Should().Throw<ArgumentException>();
    }
}
