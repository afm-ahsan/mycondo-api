using AwesomeAssertions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Domain.UnitTests.Features.Leasing.OccupancyRegistrationWorkerAssignments;

public class OccupancyRegistrationWorkerAssignmentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly OccupancyRegistrationId RegistrationId = OccupancyRegistrationId.New();
    private static readonly DomesticWorkerProfileId WorkerId = DomesticWorkerProfileId.New();

    [Fact]
    public void Assign_Starts_Active()
    {
        OccupancyRegistrationWorkerAssignment assignment =
            OccupancyRegistrationWorkerAssignment.Assign(TenantId, RegistrationId, WorkerId, Now);

        assignment.IsActive.Should().BeTrue();
        assignment.AssignedAtUtc.Should().Be(Now);
        assignment.EndedAtUtc.Should().BeNull();
    }

    [Fact]
    public void End_Sets_Inactive_And_EndedAtUtc()
    {
        OccupancyRegistrationWorkerAssignment assignment =
            OccupancyRegistrationWorkerAssignment.Assign(TenantId, RegistrationId, WorkerId, Now);
        DateTimeOffset endedAt = Now.AddDays(1);

        assignment.End(endedAt);

        assignment.IsActive.Should().BeFalse();
        assignment.EndedAtUtc.Should().Be(endedAt);
    }

    [Fact]
    public void End_Is_Idempotent()
    {
        OccupancyRegistrationWorkerAssignment assignment =
            OccupancyRegistrationWorkerAssignment.Assign(TenantId, RegistrationId, WorkerId, Now);
        assignment.End(Now.AddDays(1));

        assignment.End(Now.AddDays(2));

        assignment.EndedAtUtc.Should().Be(Now.AddDays(1));
    }

    [Fact]
    public void Assign_Throws_When_TenantId_Empty()
    {
        Action act = () => OccupancyRegistrationWorkerAssignment.Assign(Guid.Empty, RegistrationId, WorkerId, Now);

        act.Should().Throw<ArgumentException>();
    }
}
