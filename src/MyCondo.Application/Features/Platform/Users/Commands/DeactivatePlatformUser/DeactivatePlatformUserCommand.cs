using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Users.Commands.DeactivatePlatformUser;

public sealed record DeactivatePlatformUserCommand(Guid PlatformUserId) : IRequest, ILifecycleWriteOperation;
