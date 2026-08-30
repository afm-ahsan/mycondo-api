using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;

public sealed record GetRolesForTenantQuery : IRequest<List<RoleSummaryDto>>, ILifecycleReadOperation;
