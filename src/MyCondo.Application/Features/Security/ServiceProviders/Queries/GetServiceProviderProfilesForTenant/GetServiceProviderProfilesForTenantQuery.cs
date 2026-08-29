using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.ServiceProviders.Queries.GetServiceProviderProfilesForTenant;

public sealed record GetServiceProviderProfilesForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<ServiceProviderProfileDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "security.service_providers";
}
