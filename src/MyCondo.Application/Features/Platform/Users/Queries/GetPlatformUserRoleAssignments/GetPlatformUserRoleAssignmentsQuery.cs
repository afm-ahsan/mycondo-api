using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Users.DTOs;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUserRoleAssignments;

public sealed record GetPlatformUserRoleAssignmentsQuery(Guid PlatformUserId)
    : IRequest<List<PlatformUserRoleAssignmentDto>>, ILifecycleReadOperation;
