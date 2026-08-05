using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Queries.GetResidentsForTenant;

public sealed class GetResidentsForTenantQueryHandler(
    IResidentRepository residents,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetResidentsForTenantQuery, PagedResult<ResidentDto>>
{
    public async ValueTask<PagedResult<ResidentDto>> Handle(GetResidentsForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<Resident> result = await residents.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<ResidentDto> items = result.Items
            .Select(r => new ResidentDto(
                r.Id.Value, r.FlatId.Value, r.FullName, r.Phone, r.Email, r.ResidentType.ToString()))
            .ToList();

        return new PagedResult<ResidentDto>(items, result.Page, result.PageSize, result.Total);
    }
}
