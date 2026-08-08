using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;

public sealed class GetConsumptionSummaryReportQueryHandler(
    IReadingRepository readings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetConsumptionSummaryReportQuery, IReadOnlyList<ConsumptionSummaryLineDto>>
{
    public async ValueTask<IReadOnlyList<ConsumptionSummaryLineDto>> Handle(
        GetConsumptionSummaryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        UtilityType? utilityType = query.UtilityType is null ? null : Enum.Parse<UtilityType>(query.UtilityType);

        IReadOnlyList<ConsumptionSummaryLine> lines = await readings.GetConsumptionSummaryAsync(
            tenantId, buildingId, utilityType, query.FromDate, query.ToDate, cancellationToken);

        return lines
            .Select(l => new ConsumptionSummaryLineDto(l.UtilityType.ToString(), l.TotalConsumptionUnits, l.ReadingCount))
            .ToList();
    }
}
