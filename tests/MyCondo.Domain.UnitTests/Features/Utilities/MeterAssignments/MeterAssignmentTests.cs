using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.MeterAssignments.Exceptions;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Domain.UnitTests.Features.Utilities.MeterAssignments;

public class MeterAssignmentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly MeterId MeterId = MeterId.New();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void Assign_Starts_Open_With_No_AssignedToUtc()
    {
        MeterAssignment assignment = MeterAssignment.Assign(TenantId, MeterId, FlatId, Now);

        assignment.AssignedFromUtc.Should().Be(Now);
        assignment.AssignedToUtc.Should().BeNull();
    }

    [Fact]
    public void EndAssignment_Sets_AssignedToUtc()
    {
        MeterAssignment assignment = MeterAssignment.Assign(TenantId, MeterId, FlatId, Now);

        assignment.EndAssignment(Now.AddDays(30));

        assignment.AssignedToUtc.Should().Be(Now.AddDays(30));
    }

    [Fact]
    public void EndAssignment_Throws_When_Already_Ended()
    {
        MeterAssignment assignment = MeterAssignment.Assign(TenantId, MeterId, FlatId, Now);
        assignment.EndAssignment(Now.AddDays(30));

        Action act = () => assignment.EndAssignment(Now.AddDays(60));

        act.Should().Throw<MeterAssignmentAlreadyEndedException>();
    }
}
