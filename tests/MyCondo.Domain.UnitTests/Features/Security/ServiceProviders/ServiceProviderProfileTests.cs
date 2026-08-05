using AwesomeAssertions;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Domain.UnitTests.Features.Security.ServiceProviders;

public class ServiceProviderProfileTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Register_Starts_Active_And_Unverified()
    {
        ServiceProviderProfile profile = ServiceProviderProfile.Register(
            TenantId, "John Teacher", "01700000000", ServiceProviderType.Teacher, "Math tutoring", null, null, Now);

        profile.Status.Should().Be(RecurringAccessProfileStatus.Active);
        profile.VerificationStatus.Should().Be(VerificationStatus.Unverified);
        profile.ServiceDescription.Should().Be("Math tutoring");
    }

    [Fact]
    public void Block_Then_Reactivate_Round_Trips_Status()
    {
        ServiceProviderProfile profile = ServiceProviderProfile.Register(
            TenantId, "John Teacher", "01700000000", ServiceProviderType.Teacher, null, null, null, Now);

        profile.Block("Complaint received");
        profile.Status.Should().Be(RecurringAccessProfileStatus.Blocked);

        profile.Reactivate();
        profile.Status.Should().Be(RecurringAccessProfileStatus.Active);
        profile.StatusReason.Should().BeNull();
    }
}
