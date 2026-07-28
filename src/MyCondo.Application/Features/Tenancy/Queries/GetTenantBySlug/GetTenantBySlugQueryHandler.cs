using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

public sealed class GetTenantBySlugQueryHandler(
    ITenantRepository tenants
) : IRequestHandler<GetTenantBySlugQuery, TenantSummaryDto>
{
    public async ValueTask<TenantSummaryDto> Handle(GetTenantBySlugQuery query, CancellationToken cancellationToken)
    {
        string normalizedSlug = query.Slug.Trim().ToLowerInvariant();

        Tenant tenant = await tenants.GetBySlugAsync(normalizedSlug, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), normalizedSlug);

        return new TenantSummaryDto(
            TenantId: tenant.Id.Value,
            Name: tenant.Name,
            Slug: tenant.Slug,
            Status: tenant.Status.ToString());
    }
}
