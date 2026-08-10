using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationSummaryStats;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetOrganizationSummaryStats;

public class GetOrganizationSummaryStatsQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetOrganizationSummaryStatsQueryHandlerTests() => _clock.UtcNow.Returns(NowUtc);

    private GetOrganizationSummaryStatsQueryHandler CreateHandler() => new(_tenants, _clock);

    [Fact]
    public async Task Counts_Organizations_By_Status_And_Recency()
    {
        Tenant active1 = Tenant.Provision("A", "a", NowUtc.AddDays(-30));
        active1.Activate(NowUtc.AddDays(-30));
        Tenant active2 = Tenant.Provision("B", "b", NowUtc.AddDays(-1));
        active2.Activate(NowUtc.AddDays(-1));
        Tenant suspended = Tenant.Provision("C", "c", NowUtc.AddDays(-30));
        suspended.Activate(NowUtc.AddDays(-30));
        suspended.Suspend(NowUtc.AddDays(-10));
        Tenant pending = Tenant.Provision("D", "d", NowUtc);

        _tenants.GetAllAsync(Arg.Any<CancellationToken>()).Returns([active1, active2, suspended, pending]);

        OrganizationSummaryStatsDto result =
            await CreateHandler().Handle(new GetOrganizationSummaryStatsQuery(), CancellationToken.None);

        result.Total.Should().Be(4);
        result.Active.Should().Be(2);
        result.Suspended.Should().Be(1);
        result.PendingActivation.Should().Be(1);
        result.RecentlyCreated.Should().Be(2); // active2 (created -1d) and pending (created now)
    }
}
