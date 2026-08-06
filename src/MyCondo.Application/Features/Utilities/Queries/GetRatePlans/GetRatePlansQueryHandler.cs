using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Application.Features.Utilities.Queries.GetRatePlans;

public sealed class GetRatePlansQueryHandler(
    IRatePlanRepository ratePlans,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetRatePlansQuery, PagedResult<RatePlanDto>>
{
    public async ValueTask<PagedResult<RatePlanDto>> Handle(GetRatePlansQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(query.BuildingId);
        UtilityType? utilityType = query.UtilityType is null ? null : Enum.Parse<UtilityType>(query.UtilityType);

        PagedResult<RatePlan> result = await ratePlans.SearchAsync(
            tenantId, buildingId, utilityType, query.Page, query.PageSize, cancellationToken);

        List<RatePlanDto> items = [];
        foreach (RatePlan plan in result.Items)
        {
            IReadOnlyList<RateSlab> slabs = await ratePlans.GetSlabsForPlanAsync(plan.Id, cancellationToken);
            items.Add(plan.ToDto(slabs));
        }

        return new PagedResult<RatePlanDto>(items, result.Page, result.PageSize, result.Total);
    }
}
