using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest, ILifecycleWriteOperation;
