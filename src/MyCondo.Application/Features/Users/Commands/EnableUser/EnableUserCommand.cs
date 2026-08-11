using Mediator;

namespace MyCondo.Application.Features.Users.Commands.EnableUser;

public sealed record EnableUserCommand(Guid UserId) : IRequest;
