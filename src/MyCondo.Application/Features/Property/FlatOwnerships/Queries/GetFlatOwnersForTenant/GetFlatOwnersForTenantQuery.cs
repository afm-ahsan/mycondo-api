using Mediator;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnersForTenant;

public sealed record GetFlatOwnersForTenantQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<FlatOwnerRegisterDto>>;
