using Mediator;

namespace MyCondo.Application.Features.Users.Queries.GetUserRoleAssignments;

public sealed record GetUserRoleAssignmentsQuery(Guid UserId) : IRequest<List<UserRoleAssignmentDto>>;
