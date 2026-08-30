using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Roles.Queries.GetRoleAssignments;

public sealed record GetRoleAssignmentsQuery(Guid RoleId) : IRequest<List<RoleAssignmentDto>>, ILifecycleReadOperation;
