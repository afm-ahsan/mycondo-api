using Mediator;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.ListOrganizations;

public sealed class ListOrganizationsQueryHandler(
    ITenantRepository tenants
) : IRequestHandler<ListOrganizationsQuery, IReadOnlyList<TenantSummaryDto>>
{
    public async ValueTask<IReadOnlyList<TenantSummaryDto>> Handle(
        ListOrganizationsQuery query, CancellationToken cancellationToken)
    {
        List<Tenant> all = await tenants.GetAllAsync(cancellationToken);

        return all
            .Select(t => new TenantSummaryDto(t.Id.Value, t.Name, t.Slug, t.Status.ToString()))
            .ToList();
    }
}
