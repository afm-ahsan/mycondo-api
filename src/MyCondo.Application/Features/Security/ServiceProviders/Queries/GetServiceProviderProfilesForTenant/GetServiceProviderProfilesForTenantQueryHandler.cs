using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Application.Features.Security.ServiceProviders.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviders.Queries.GetServiceProviderProfilesForTenant;

public sealed class GetServiceProviderProfilesForTenantQueryHandler(
    IServiceProviderProfileRepository profiles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetServiceProviderProfilesForTenantQuery, PagedResult<ServiceProviderProfileDto>>
{
    public async ValueTask<PagedResult<ServiceProviderProfileDto>> Handle(GetServiceProviderProfilesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<ServiceProviderProfile> result = await profiles.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<ServiceProviderProfileDto> items = result.Items.Select(p => p.ToDto()).ToList();

        return new PagedResult<ServiceProviderProfileDto>(items, result.Page, result.PageSize, result.Total);
    }
}
