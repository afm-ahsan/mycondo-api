using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.EndTenantFeatureOverride;

public sealed record EndTenantFeatureOverrideCommand(
    Guid OrganizationId,
    Guid OverrideId,
    DateTimeOffset EffectiveUntil
) : IRequest;
