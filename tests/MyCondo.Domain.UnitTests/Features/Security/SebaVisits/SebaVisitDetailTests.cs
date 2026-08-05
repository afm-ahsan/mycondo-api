using AwesomeAssertions;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.SebaVisits;

namespace MyCondo.Domain.UnitTests.Features.Security.SebaVisits;

public class SebaVisitDetailTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly AccessSessionId AccessSessionId = AccessSessionId.New();

    [Fact]
    public void Record_Trims_VisitorFullName()
    {
        SebaVisitDetail detail = SebaVisitDetail.Record(
            TenantId, AccessSessionId, "  Jane Public  ", "017", "ABC Corp", "Complaints Dept", "T-001",
            "Complaint", Guid.NewGuid(), Now);

        detail.VisitorFullName.Should().Be("Jane Public");
        detail.Acknowledged.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_Throws_When_VisitorFullName_Is_Blank(string fullName)
    {
        Action act = () => SebaVisitDetail.Record(
            TenantId, AccessSessionId, fullName, null, null, null, null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordOutcome_Sets_Outcome_And_Acknowledged()
    {
        SebaVisitDetail detail = SebaVisitDetail.Record(
            TenantId, AccessSessionId, "Jane Public", null, null, null, null, null, null, Now);

        detail.RecordOutcome("Complaint resolved on the spot", true);

        detail.ServiceOutcome.Should().Be("Complaint resolved on the spot");
        detail.Acknowledged.Should().BeTrue();
    }
}
