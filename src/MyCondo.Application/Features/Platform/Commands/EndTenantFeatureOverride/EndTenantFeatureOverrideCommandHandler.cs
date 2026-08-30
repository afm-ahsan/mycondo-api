using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Application.Features.Platform.Commands.EndTenantFeatureOverride;

/// <summary>
/// Ends an active <see cref="TenantFeatureOverride"/> early by shortening its window (ADR-033 Task
/// 13C) — pre-validates the requested end time against the override's current window as a friendly
/// <see cref="ConflictException"/> (409) before calling <see cref="TenantFeatureOverride.End"/>, whose
/// own checks are defense-in-depth. No overlap pre-check is needed: shortening a window can never
/// introduce a new overlap.
/// </summary>
public sealed class EndTenantFeatureOverrideCommandHandler(
    ITenantFeatureOverrideRepository tenantFeatureOverrides,
    IUnitOfWork unitOfWork
) : IRequestHandler<EndTenantFeatureOverrideCommand>
{
    public async ValueTask<Unit> Handle(EndTenantFeatureOverrideCommand command, CancellationToken cancellationToken)
    {
        TenantFeatureOverrideId id = new(command.OverrideId);

        TenantFeatureOverride @override = await tenantFeatureOverrides.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantFeatureOverride), command.OverrideId);

        if (@override.TenantId != command.OrganizationId)
        {
            throw new NotFoundException(nameof(TenantFeatureOverride), command.OverrideId);
        }

        if (command.EffectiveUntil <= @override.EffectiveFrom)
        {
            throw new ConflictException("EffectiveUntil must be after the override's EffectiveFrom.");
        }

        if (@override.EffectiveUntil is not null && command.EffectiveUntil >= @override.EffectiveUntil.Value)
        {
            throw new ConflictException(
                "EffectiveUntil must be before the override's current EffectiveUntil to end it early.");
        }

        @override.End(command.EffectiveUntil);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
