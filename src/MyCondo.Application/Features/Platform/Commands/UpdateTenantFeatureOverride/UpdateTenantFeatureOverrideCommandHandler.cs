using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Application.Features.Platform.Commands.UpdateTenantFeatureOverride;

/// <summary>
/// Edits an existing <see cref="TenantFeatureOverride"/>'s terms (ADR-033 Task 13C). Re-resolves the
/// target <see cref="FeatureDefinition"/> so <see cref="TenantFeatureOverride.Update"/> can re-run
/// Core/Reserved eligibility — a feature's catalogue status can change after the override was first
/// created. <see cref="UpdateTenantFeatureOverrideCommand.OrganizationId"/> is checked against the
/// loaded override's own tenant so a request nested under the wrong organization's URL 404s instead of
/// silently editing another tenant's override.
/// </summary>
public sealed class UpdateTenantFeatureOverrideCommandHandler(
    ITenantFeatureOverrideRepository tenantFeatureOverrides,
    IFeatureDefinitionRepository featureDefinitions,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateTenantFeatureOverrideCommand>
{
    public async ValueTask<Unit> Handle(UpdateTenantFeatureOverrideCommand command, CancellationToken cancellationToken)
    {
        TenantFeatureOverrideId id = new(command.OverrideId);

        TenantFeatureOverride @override = await tenantFeatureOverrides.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantFeatureOverride), command.OverrideId);

        if (@override.TenantId != command.OrganizationId)
        {
            throw new NotFoundException(nameof(TenantFeatureOverride), command.OverrideId);
        }

        FeatureDefinition feature = (await featureDefinitions.GetAllAsync(cancellationToken))
                .SingleOrDefault(f => f.Id == @override.FeatureId)
            ?? throw new NotFoundException(nameof(FeatureDefinition), @override.FeatureId.Value);

        bool hasOverlap = await tenantFeatureOverrides.HasOverlappingOverrideAsync(
            @override.TenantId, @override.FeatureId, command.EffectiveFrom, command.EffectiveUntil,
            excludeId: id, cancellationToken);
        if (hasOverlap)
        {
            throw new ConflictException(
                $"An overlapping feature override already exists for feature '{feature.Key}' on this organization.");
        }

        @override.Update(feature, command.Enabled, command.EffectiveFrom, command.EffectiveUntil, command.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
