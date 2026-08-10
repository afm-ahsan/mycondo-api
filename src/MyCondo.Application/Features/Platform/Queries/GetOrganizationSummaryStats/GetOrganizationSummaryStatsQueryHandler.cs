using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationSummaryStats;

public sealed class GetOrganizationSummaryStatsQueryHandler(
    ITenantRepository tenants,
    IClock clock
) : IRequestHandler<GetOrganizationSummaryStatsQuery, OrganizationSummaryStatsDto>
{
    public async ValueTask<OrganizationSummaryStatsDto> Handle(
        GetOrganizationSummaryStatsQuery query, CancellationToken cancellationToken)
    {
        List<Tenant> all = await tenants.GetAllAsync(cancellationToken);
        DateTimeOffset sevenDaysAgo = clock.UtcNow.AddDays(-7);

        return new OrganizationSummaryStatsDto(
            Total: all.Count,
            Active: all.Count(t => t.Status == TenantStatus.Active),
            Suspended: all.Count(t => t.Status == TenantStatus.Suspended),
            PendingActivation: all.Count(t => t.Status == TenantStatus.PendingActivation),
            RecentlyCreated: all.Count(t => t.CreatedAtUtc >= sevenDaysAgo));
    }
}
