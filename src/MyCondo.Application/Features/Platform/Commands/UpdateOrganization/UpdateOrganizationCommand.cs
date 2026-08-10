using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid OrganizationId,
    string Name,
    string? Code
) : IRequest;
