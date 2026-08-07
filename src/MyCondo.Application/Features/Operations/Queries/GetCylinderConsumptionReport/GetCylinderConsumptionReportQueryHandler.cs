using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderConsumptionReport;

public sealed class GetCylinderConsumptionReportQueryHandler(
    ICylinderStockMovementRepository movements,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCylinderConsumptionReportQuery, IReadOnlyList<CylinderConsumptionReportLineDto>>
{
    public async ValueTask<IReadOnlyList<CylinderConsumptionReportLineDto>> Handle(
        GetCylinderConsumptionReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateTimeOffset fromUtc = new(query.FromDate, TimeOnly.MinValue, TimeSpan.Zero);
        DateTimeOffset toUtc = new(query.ToDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);

        IReadOnlyList<string> cylinderTypes = string.IsNullOrWhiteSpace(query.CylinderType)
            ? await movements.ListDistinctCylinderTypesAsync(tenantId, cancellationToken)
            : [query.CylinderType];

        List<CylinderConsumptionReportLineDto> lines = [];
        foreach (string cylinderType in cylinderTypes)
        {
            IReadOnlyList<CylinderStockMovement> periodMovements = await movements.GetForPeriodAsync(
                tenantId, cylinderType, fromUtc, toUtc, cancellationToken);

            int totalReceived = periodMovements.Where(m => m.MovementType == CylinderStockMovementType.Receipt).Sum(m => m.Quantity);
            int totalIssued = -periodMovements.Where(m => m.MovementType == CylinderStockMovementType.Issue).Sum(m => m.Quantity);
            int totalEmptyReturned = -periodMovements.Where(m => m.MovementType == CylinderStockMovementType.EmptyReturn).Sum(m => m.Quantity);
            int netChange = periodMovements.Sum(m => m.Quantity);

            lines.Add(new CylinderConsumptionReportLineDto(cylinderType, totalReceived, totalIssued, totalEmptyReturned, netChange));
        }

        return lines;
    }
}
