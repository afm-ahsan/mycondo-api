using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.CreateTenantFeatureOverride;

public sealed record CreateTenantFeatureOverrideCommand(
    Guid OrganizationId,
    string FeatureKey,
    bool Enabled,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveUntil,
    string? Reason,
    Guid CreatedBy
) : IRequest<Guid>;
