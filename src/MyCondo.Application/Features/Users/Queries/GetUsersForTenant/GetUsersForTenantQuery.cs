using Mediator;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

public sealed record GetUsersForTenantQuery(
    string? SearchText,
    Guid? RoleId,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<UserSummaryDto>>;
