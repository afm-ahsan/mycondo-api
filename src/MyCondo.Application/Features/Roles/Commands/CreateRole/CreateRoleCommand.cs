using Mediator;

namespace MyCondo.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(
    string Name,
    string Description
) : IRequest<CreateRoleResult>;
