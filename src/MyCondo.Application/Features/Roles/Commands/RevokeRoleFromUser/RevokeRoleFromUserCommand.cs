using Mediator;

namespace MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;

public sealed record RevokeRoleFromUserCommand(
    Guid RoleId,
    Guid UserId,
    Guid? BuildingId
) : IRequest;
