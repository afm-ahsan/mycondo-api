using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Roles.DTOs;

namespace MyCondo.Application.Features.Platform.Roles.Queries.GetPlatformRoles;

/// <summary>
/// Lists the platform-scope roles available to grant/revoke via
/// <c>AssignPlatformRoleToUserCommand</c>/<c>RevokePlatformRoleFromUserCommand</c>, which take a
/// <c>RoleId</c> the caller otherwise has no way to discover for a user who does not already hold it.
/// Gated under the existing <c>platform.user.view</c> permission rather than a new catalogue entry —
/// this endpoint exists solely to support the Platform Users role-assignment UI (mycondo-docs
/// ADR-036), not as a general platform-role directory.
/// </summary>
public sealed record GetPlatformRolesQuery : IRequest<List<PlatformRoleSummaryDto>>, ILifecycleReadOperation;
