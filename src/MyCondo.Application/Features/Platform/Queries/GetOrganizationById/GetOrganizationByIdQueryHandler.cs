using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationById;

public sealed class GetOrganizationByIdQueryHandler(
    ITenantRepository tenants
) : IRequestHandler<GetOrganizationByIdQuery, TenantSummaryDto>
{
    public async ValueTask<TenantSummaryDto> Handle(
        GetOrganizationByIdQuery query, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(query.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), query.OrganizationId);

        return new TenantSummaryDto(tenant.Id.Value, tenant.Name, tenant.Slug, tenant.Status.ToString());
    }
}
