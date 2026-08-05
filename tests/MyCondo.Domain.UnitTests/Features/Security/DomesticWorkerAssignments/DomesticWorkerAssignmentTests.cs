using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Domain.UnitTests.Features.Security.DomesticWorkerAssignments;

public class DomesticWorkerAssignmentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DomesticWorkerProfileId WorkerId = DomesticWorkerProfileId.New();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void Create_Defaults_AllowedDays_To_All_When_None_Given()
    {
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, Now, null, DaysOfWeekFlags.None, null, null, Now);

        assignment.AllowedDays.Should().Be(DaysOfWeekFlags.All);
        assignment.ApprovedByResident.Should().BeFalse();
        assignment.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_When_ValidToUtc_Before_ValidFromUtc()
    {
        Action act = () => DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, Now, Now.AddDays(-1), DaysOfWeekFlags.All, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApproveByResident_Sets_Flag()
    {
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, Now, null, DaysOfWeekFlags.All, null, null, Now);

        assignment.ApproveByResident();

        assignment.ApprovedByResident.Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyValid_False_When_Not_Approved()
    {
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, Now.AddDays(-1), null, DaysOfWeekFlags.All, null, null, Now);

        assignment.IsCurrentlyValid(Now, Now).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_False_When_Deactivated()
    {
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, Now.AddDays(-1), null, DaysOfWeekFlags.All, null, null, Now);
        assignment.ApproveByResident();
        assignment.Deactivate();

        assignment.IsCurrentlyValid(Now, Now).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_False_When_Outside_ValidToUtc()
    {
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, Now.AddDays(-10), Now.AddDays(-1), DaysOfWeekFlags.All, null, null, Now);
        assignment.ApproveByResident();

        assignment.IsCurrentlyValid(Now, Now).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_False_When_Today_Not_Allowed()
    {
        DateTimeOffset local = new(2026, 8, 10, 10, 0, 0, TimeSpan.Zero); // Monday
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, local.AddDays(-1), null, DaysOfWeekFlags.Tuesday, null, null, Now);
        assignment.ApproveByResident();

        assignment.IsCurrentlyValid(local, local).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_True_When_Approved_Within_Window_And_Allowed_Day()
    {
        DateTimeOffset local = new(2026, 8, 10, 10, 0, 0, TimeSpan.Zero); // Monday
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, local.AddDays(-1), null, DaysOfWeekFlags.Monday,
            new TimeOnly(8, 0), new TimeOnly(18, 0), Now);
        assignment.ApproveByResident();

        assignment.IsCurrentlyValid(local, local).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyValid_False_When_Outside_Allowed_Time_Window()
    {
        DateTimeOffset local = new(2026, 8, 10, 20, 0, 0, TimeSpan.Zero); // Monday, 8 PM
        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            TenantId, WorkerId, FlatId, local.AddDays(-1), null, DaysOfWeekFlags.Monday,
            new TimeOnly(8, 0), new TimeOnly(18, 0), Now);
        assignment.ApproveByResident();

        assignment.IsCurrentlyValid(local, local).Should().BeFalse();
    }
}
