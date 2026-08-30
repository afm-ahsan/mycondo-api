using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Users.Commands.ActivatePlatformUser;

public sealed record ActivatePlatformUserCommand(Guid PlatformUserId) : IRequest, ILifecycleWriteOperation;
