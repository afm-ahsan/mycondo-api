using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Users.Commands.AssignPlatformRoleToUser;

public sealed record AssignPlatformRoleToUserCommand(
    Guid RoleId,
    Guid PlatformUserId
) : IRequest, ILifecycleWriteOperation;
