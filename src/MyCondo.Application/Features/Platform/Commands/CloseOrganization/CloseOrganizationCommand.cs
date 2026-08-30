using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.CloseOrganization;

public sealed record CloseOrganizationCommand(Guid OrganizationId) : IRequest;
