using Mediator;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Residents.Queries.GetResidentsForTenant;

public sealed record GetResidentsForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<ResidentDto>>;
