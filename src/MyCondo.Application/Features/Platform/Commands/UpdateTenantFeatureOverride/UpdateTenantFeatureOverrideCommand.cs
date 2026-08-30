using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.UpdateTenantFeatureOverride;

public sealed record UpdateTenantFeatureOverrideCommand(
    Guid OrganizationId,
    Guid OverrideId,
    bool Enabled,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveUntil,
    string? Reason
) : IRequest;
