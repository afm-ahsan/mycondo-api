using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Domain.UnitTests.Features.Security.ServiceProviderAssignments;

public class ServiceProviderAssignmentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly ServiceProviderProfileId ProviderId = ServiceProviderProfileId.New();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void Create_Defaults_AllowedDays_To_All_When_None_Given()
    {
        ServiceProviderAssignment assignment = ServiceProviderAssignment.Create(
            TenantId, ProviderId, FlatId, Now, null, DaysOfWeekFlags.None, null, null, Now);

        assignment.AllowedDays.Should().Be(DaysOfWeekFlags.All);
        assignment.ApprovedByResident.Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyValid_True_When_Approved_Within_Window_And_Allowed_Day()
    {
        DateTimeOffset local = new(2026, 8, 10, 10, 0, 0, TimeSpan.Zero); // Monday
        ServiceProviderAssignment assignment = ServiceProviderAssignment.Create(
            TenantId, ProviderId, FlatId, local.AddDays(-1), null, DaysOfWeekFlags.Monday,
            new TimeOnly(8, 0), new TimeOnly(18, 0), Now);
        assignment.ApproveByResident();

        assignment.IsCurrentlyValid(local, local).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyValid_False_When_Not_Approved()
    {
        ServiceProviderAssignment assignment = ServiceProviderAssignment.Create(
            TenantId, ProviderId, FlatId, Now.AddDays(-1), null, DaysOfWeekFlags.All, null, null, Now);

        assignment.IsCurrentlyValid(Now, Now).Should().BeFalse();
    }
}
