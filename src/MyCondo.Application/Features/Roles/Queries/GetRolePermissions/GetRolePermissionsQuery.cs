using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;

namespace MyCondo.Application.Features.Roles.Queries.GetRolePermissions;

public sealed record GetRolePermissionsQuery(Guid RoleId) : IRequest<List<PermissionDto>>, ILifecycleReadOperation;
