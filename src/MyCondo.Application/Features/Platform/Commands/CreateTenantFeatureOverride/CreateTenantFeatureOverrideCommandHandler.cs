using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.CreateTenantFeatureOverride;

/// <summary>
/// Creates a platform-administered <see cref="TenantFeatureOverride"/> for an organization (ADR-033
/// Task 13C) — Core/Reserved rejection is enforced by <see cref="TenantFeatureOverride.Create"/> itself
/// (<see cref="TenantFeatureOverrideEligibility"/>), not duplicated here. This handler owns only the
/// application-layer overlap pre-check ahead of the authoritative DB EXCLUDE constraint, mirroring
/// <c>ChangeOrganizationSubscriptionCommandHandler</c>'s own shape.
/// </summary>
public sealed class CreateTenantFeatureOverrideCommandHandler(
    ITenantRepository tenants,
    IFeatureDefinitionRepository featureDefinitions,
    ITenantFeatureOverrideRepository tenantFeatureOverrides,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CreateTenantFeatureOverrideCommandHandler> logger
) : IRequestHandler<CreateTenantFeatureOverrideCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTenantFeatureOverrideCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        FeatureDefinition feature = (await featureDefinitions.GetAllAsync(cancellationToken))
                .SingleOrDefault(f => string.Equals(f.Key, command.FeatureKey, StringComparison.Ordinal))
            ?? throw new NotFoundException(nameof(FeatureDefinition), command.FeatureKey);

        bool hasOverlap = await tenantFeatureOverrides.HasOverlappingOverrideAsync(
            tenant.Id.Value, feature.Id, command.EffectiveFrom, command.EffectiveUntil, excludeId: null, cancellationToken);
        if (hasOverlap)
        {
            throw new ConflictException(
                $"An overlapping feature override already exists for feature '{feature.Key}' on this organization.");
        }

        TenantFeatureOverride @override = TenantFeatureOverride.Create(
            tenant.Id.Value, feature, command.Enabled, command.EffectiveFrom, command.EffectiveUntil,
            command.Reason, command.CreatedBy, clock.UtcNow);

        tenantFeatureOverrides.Add(@override);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Tenant feature override {OverrideId} created for organization {TenantId} feature {FeatureKey}",
            @override.Id, tenant.Id, feature.Key);

        return @override.Id.Value;
    }
}
