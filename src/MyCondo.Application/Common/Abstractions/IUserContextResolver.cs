using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Resolves the authenticated-user view of a <see cref="User"/> aggregate — including
/// effective role names, permission strings, and building scope. Backed by EF queries
/// across <c>Roles</c> and <c>RoleAssignments</c>; the implementation lives in Infrastructure.
/// </summary>
public interface IUserContextResolver
{
    Task<AuthenticatedUserDto> ResolveAsync(User user, CancellationToken cancellationToken);
    Task<UserProfileDto> ResolveProfileAsync(User user, CancellationToken cancellationToken);
}
