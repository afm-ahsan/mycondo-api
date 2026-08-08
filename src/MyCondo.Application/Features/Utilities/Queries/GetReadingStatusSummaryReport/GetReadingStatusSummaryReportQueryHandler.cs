using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;

public sealed class GetReadingStatusSummaryReportQueryHandler(
    IReadingRepository readings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetReadingStatusSummaryReportQuery, IReadOnlyList<ReadingStatusSummaryLineDto>>
{
    public async ValueTask<IReadOnlyList<ReadingStatusSummaryLineDto>> Handle(
        GetReadingStatusSummaryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        UtilityType? utilityType = query.UtilityType is null ? null : Enum.Parse<UtilityType>(query.UtilityType);

        IReadOnlyList<ReadingStatusSummaryLine> lines = await readings.GetStatusSummaryAsync(
            tenantId, buildingId, utilityType, cancellationToken);

        return lines
            .Select(l => new ReadingStatusSummaryLineDto(l.UtilityType.ToString(), l.Status.ToString(), l.Count))
            .ToList();
    }
}
