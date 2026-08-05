using Mediator;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Queries.GetDomesticWorkerProfilesForTenant;

public sealed record GetDomesticWorkerProfilesForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<DomesticWorkerProfileDto>>;
