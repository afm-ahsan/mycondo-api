using Mediator;

namespace MyCondo.Application.Features.Roles.Commands.DeactivateRole;

public sealed record DeactivateRoleCommand(Guid RoleId) : IRequest;
