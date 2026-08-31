using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Users.Commands.RevokePlatformRoleFromUser;

public sealed record RevokePlatformRoleFromUserCommand(
    Guid RoleId,
    Guid PlatformUserId
) : IRequest, ILifecycleWriteOperation;
