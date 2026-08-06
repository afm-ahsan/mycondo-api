using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeters;

public sealed class GetMetersQueryHandler(
    IMeterRepository meters,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMetersQuery, PagedResult<MeterDto>>
{
    public async ValueTask<PagedResult<MeterDto>> Handle(GetMetersQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(query.BuildingId);
        UtilityType? utilityType = query.UtilityType is null ? null : Enum.Parse<UtilityType>(query.UtilityType);

        PagedResult<Meter> result = await meters.SearchAsync(
            tenantId, buildingId, utilityType, query.Page, query.PageSize, cancellationToken);

        List<MeterDto> items = result.Items.Select(m => m.ToDto()).ToList();

        return new PagedResult<MeterDto>(items, result.Page, result.PageSize, result.Total);
    }
}
