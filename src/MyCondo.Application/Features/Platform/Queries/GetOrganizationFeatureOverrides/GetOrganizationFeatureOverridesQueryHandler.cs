using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationFeatureOverrides;

/// <summary>Read-only platform view of an organization's configured (not resolved-effective)
/// <see cref="TenantFeatureOverride"/> rows (ADR-033 Task 13C) — the row identifiers a platform admin
/// needs before they can call Update/End on a specific override.</summary>
public sealed class GetOrganizationFeatureOverridesQueryHandler(
    ITenantRepository tenants,
    ITenantFeatureOverrideRepository tenantFeatureOverrides,
    IFeatureDefinitionRepository featureDefinitions
) : IRequestHandler<GetOrganizationFeatureOverridesQuery, List<TenantFeatureOverrideDto>>
{
    public async ValueTask<List<TenantFeatureOverrideDto>> Handle(
        GetOrganizationFeatureOverridesQuery query, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(query.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), query.OrganizationId);

        List<TenantFeatureOverride> overrides =
            await tenantFeatureOverrides.GetForTenantAsync(tenant.Id.Value, cancellationToken);

        Dictionary<FeatureDefinitionId, FeatureDefinition> featuresById =
            (await featureDefinitions.GetAllAsync(cancellationToken)).ToDictionary(f => f.Id);

        return overrides
            .OrderByDescending(o => o.CreatedAt)
            .Select(o =>
            {
                FeatureDefinition feature = featuresById[o.FeatureId];
                return new TenantFeatureOverrideDto(
                    o.Id.Value, feature.Key, feature.Name, o.Enabled, o.EffectiveFrom, o.EffectiveUntil,
                    o.Reason, o.CreatedBy, o.CreatedAt);
            })
            .ToList();
    }
}
