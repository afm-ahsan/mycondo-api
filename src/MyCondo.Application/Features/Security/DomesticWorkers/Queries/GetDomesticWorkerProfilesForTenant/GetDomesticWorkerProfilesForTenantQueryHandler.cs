using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkers.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Queries.GetDomesticWorkerProfilesForTenant;

public sealed class GetDomesticWorkerProfilesForTenantQueryHandler(
    IDomesticWorkerProfileRepository profiles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetDomesticWorkerProfilesForTenantQuery, PagedResult<DomesticWorkerProfileDto>>
{
    public async ValueTask<PagedResult<DomesticWorkerProfileDto>> Handle(GetDomesticWorkerProfilesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<DomesticWorkerProfile> result = await profiles.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<DomesticWorkerProfileDto> items = result.Items.Select(p => p.ToDto()).ToList();

        return new PagedResult<DomesticWorkerProfileDto>(items, result.Page, result.PageSize, result.Total);
    }
}
