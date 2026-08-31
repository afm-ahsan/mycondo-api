using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Users.Commands.UpdatePlatformUser;

/// <summary>Email is deliberately not editable here — see <c>PlatformUser.ChangeDisplayName</c>.</summary>
public sealed record UpdatePlatformUserCommand(Guid PlatformUserId, string DisplayName)
    : IRequest, ILifecycleWriteOperation;
