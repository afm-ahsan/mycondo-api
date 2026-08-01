using Mediator;

namespace MyCondo.Application.Features.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest;
