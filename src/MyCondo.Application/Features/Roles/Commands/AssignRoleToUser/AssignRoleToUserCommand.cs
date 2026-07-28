using Mediator;

namespace MyCondo.Application.Features.Roles.Commands.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(
    Guid RoleId,
    Guid UserId,
    Guid? BuildingId
) : IRequest;
