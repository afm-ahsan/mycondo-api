using Mediator;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.ServiceProviders.Queries.GetServiceProviderProfilesForTenant;

public sealed record GetServiceProviderProfilesForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<ServiceProviderProfileDto>>;
