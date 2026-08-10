using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.ReactivateOrganization;

public sealed record ReactivateOrganizationCommand(Guid OrganizationId) : IRequest;
