using AwesomeAssertions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolIncidents;

namespace MyCondo.Domain.UnitTests.Features.Amenities.PoolIncidents;

public class PoolIncidentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FacilityId FacilityId = FacilityId.New();

    [Fact]
    public void Report_Creates_The_Incident()
    {
        PoolIncident incident = PoolIncident.Report(
            TenantId, FacilityId, null, Now, Guid.NewGuid(), "Slip near the deep end", PoolIncidentSeverity.Moderate,
            "First aid administered", Now);

        incident.Description.Should().Be("Slip near the deep end");
        incident.Severity.Should().Be(PoolIncidentSeverity.Moderate);
        incident.ActionTaken.Should().Be("First aid administered");
    }

    [Fact]
    public void Report_Throws_When_Description_Empty()
    {
        Action act = () => PoolIncident.Report(
            TenantId, FacilityId, null, Now, Guid.NewGuid(), "  ", PoolIncidentSeverity.Minor, null, Now);

        act.Should().Throw<ArgumentException>();
    }
}
