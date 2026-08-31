using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Users.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Queries.GetPlatformUsers;

public sealed record GetPlatformUsersQuery(
    string? SearchText,
    PlatformUserStatus? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<PlatformUserSummaryDto>>, ILifecycleReadOperation;
