using Mediator;

namespace MyCondo.Application.Features.Roles.Queries.GetRoleAssignments;

public sealed record GetRoleAssignmentsQuery(Guid RoleId) : IRequest<List<RoleAssignmentDto>>;
