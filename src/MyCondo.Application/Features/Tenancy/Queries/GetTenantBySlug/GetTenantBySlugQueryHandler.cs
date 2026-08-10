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
        string identifier = query.Slug.Trim();
        string normalizedSlug = identifier.ToLowerInvariant();

        // Tenant sign-in asks the user for their "Organization" — end users identify it by display
        // name (e.g. "Akter Residence Park"), not by the internal slug (e.g. "arp"), so the slug
        // lookup falls back to a case-insensitive exact match on the tenant's name.
        Tenant tenant = await tenants.GetBySlugAsync(normalizedSlug, cancellationToken)
            ?? await tenants.GetByNameAsync(identifier, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), identifier);

        return new TenantSummaryDto(
            TenantId: tenant.Id.Value,
            Name: tenant.Name,
            Slug: tenant.Slug,
            Status: tenant.Status.ToString());
    }
}
