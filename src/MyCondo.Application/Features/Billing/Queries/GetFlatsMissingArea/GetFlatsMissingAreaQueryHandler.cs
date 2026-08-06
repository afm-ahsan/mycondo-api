using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Queries.GetFlatsMissingArea;

public sealed class GetFlatsMissingAreaQueryHandler(
    IFlatRepository flats,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFlatsMissingAreaQuery, PagedResult<FlatMissingAreaDto>>
{
    public async ValueTask<PagedResult<FlatMissingAreaDto>> Handle(
        GetFlatsMissingAreaQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(query.BuildingId);
        PagedResult<Flat> result = await flats.SearchMissingAreaSqFtAsync(
            tenantId, buildingId, query.Page, query.PageSize, cancellationToken);

        List<FlatMissingAreaDto> items = result.Items.Select(f => f.ToMissingAreaDto()).ToList();

        return new PagedResult<FlatMissingAreaDto>(items, result.Page, result.PageSize, result.Total);
    }
}
