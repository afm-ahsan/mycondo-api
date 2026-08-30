using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Users.Commands.CreatePlatformUser;

/// <summary>
/// Creates a Platform-scope operator account. The administrator sets <see cref="Password"/> directly —
/// there is no generated-temporary-password flow, mirroring tenant <c>CreateUserCommand</c>.
/// <see cref="InitialRoleName"/> is optional; when provided and it names the SuperAdmin role, granting
/// it is itself a privileged mutation gated on <c>platform.user.manageSuperAdmins</c> (see mycondo-docs
/// ADR-035).
/// </summary>
public sealed record CreatePlatformUserCommand(
    string DisplayName,
    string Email,
    string Password,
    bool IsActive,
    string? InitialRoleName
) : IRequest<CreatePlatformUserResult>, ILifecycleWriteOperation;
