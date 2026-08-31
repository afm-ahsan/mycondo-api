using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Users.DTOs;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUserById;

public sealed record GetPlatformUserByIdQuery(Guid PlatformUserId)
    : IRequest<PlatformUserDetailDto>, ILifecycleReadOperation;
