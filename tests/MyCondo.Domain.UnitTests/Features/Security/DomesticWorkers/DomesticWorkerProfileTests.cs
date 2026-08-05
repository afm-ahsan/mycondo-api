using AwesomeAssertions;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Domain.UnitTests.Features.Security.DomesticWorkers;

public class DomesticWorkerProfileTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Register_Starts_Active_And_Unverified()
    {
        DomesticWorkerProfile profile = DomesticWorkerProfile.Register(
            TenantId, "Jane Doe", "01700000000", DomesticWorkerType.Maid, null, null, null, null, Now);

        profile.Status.Should().Be(RecurringAccessProfileStatus.Active);
        profile.VerificationStatus.Should().Be(VerificationStatus.Unverified);
        profile.Version.Should().Be(1);
    }

    [Fact]
    public void Suspend_Sets_Status_And_Reason()
    {
        DomesticWorkerProfile profile = DomesticWorkerProfile.Register(
            TenantId, "Jane Doe", "01700000000", DomesticWorkerType.Maid, null, null, null, null, Now);

        profile.Suspend("Failed background check");

        profile.Status.Should().Be(RecurringAccessProfileStatus.Suspended);
        profile.StatusReason.Should().Be("Failed background check");
    }

    [Fact]
    public void Block_Sets_Status_And_Reason()
    {
        DomesticWorkerProfile profile = DomesticWorkerProfile.Register(
            TenantId, "Jane Doe", "01700000000", DomesticWorkerType.Maid, null, null, null, null, Now);

        profile.Block("Theft reported");

        profile.Status.Should().Be(RecurringAccessProfileStatus.Blocked);
        profile.StatusReason.Should().Be("Theft reported");
    }

    [Fact]
    public void Reactivate_Clears_Status_Reason()
    {
        DomesticWorkerProfile profile = DomesticWorkerProfile.Register(
            TenantId, "Jane Doe", "01700000000", DomesticWorkerType.Maid, null, null, null, null, Now);
        profile.Suspend("Reason");

        profile.Reactivate();

        profile.Status.Should().Be(RecurringAccessProfileStatus.Active);
        profile.StatusReason.Should().BeNull();
    }

    [Fact]
    public void Verify_Sets_VerificationStatus()
    {
        DomesticWorkerProfile profile = DomesticWorkerProfile.Register(
            TenantId, "Jane Doe", "01700000000", DomesticWorkerType.Maid, null, null, null, null, Now);

        profile.Verify();

        profile.VerificationStatus.Should().Be(VerificationStatus.Verified);
    }
}
