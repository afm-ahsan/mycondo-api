using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeterStatusSummaryReport;

public sealed class GetMeterStatusSummaryReportQueryHandler(
    IMeterRepository meters,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMeterStatusSummaryReportQuery, IReadOnlyList<MeterStatusSummaryLineDto>>
{
    public async ValueTask<IReadOnlyList<MeterStatusSummaryLineDto>> Handle(
        GetMeterStatusSummaryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        UtilityType? utilityType = query.UtilityType is null ? null : Enum.Parse<UtilityType>(query.UtilityType);

        IReadOnlyList<MeterStatusSummaryLine> lines = await meters.GetStatusSummaryAsync(
            tenantId, buildingId, utilityType, cancellationToken);

        return lines
            .Select(l => new MeterStatusSummaryLineDto(l.UtilityType.ToString(), l.Status.ToString(), l.Count))
            .ToList();
    }
}
