using Mediator;

namespace MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;

public sealed record GetRolesForTenantQuery : IRequest<List<RoleSummaryDto>>;
