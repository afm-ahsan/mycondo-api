using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Resolves a <see cref="PlatformUser"/> to its effective roles + permissions — the Platform-scope
/// analogue of <see cref="IUserContextResolver"/>, simplified because there is no building-scope
/// dimension at Platform level (see <see cref="MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments.PlatformUserRoleAssignment"/>).
/// </summary>
public interface IPlatformUserContextResolver
{
    Task<PlatformAuthenticatedUserDto> ResolveAsync(PlatformUser platformUser, CancellationToken cancellationToken);
}
