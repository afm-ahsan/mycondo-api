using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierComparisonReport;

public sealed class GetSupplierComparisonReportQueryHandler(
    ICylinderPurchaseRepository purchases,
    IGasCylinderSupplierRepository suppliers,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetSupplierComparisonReportQuery, IReadOnlyList<SupplierComparisonReportLineDto>>
{
    public async ValueTask<IReadOnlyList<SupplierComparisonReportLineDto>> Handle(
        GetSupplierComparisonReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<CylinderPurchase> periodPurchases = await purchases.GetForPeriodAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);

        List<SupplierComparisonReportLineDto> lines = [];
        foreach (IGrouping<GasCylinderSupplierId, CylinderPurchase> group in periodPurchases.GroupBy(p => p.SupplierId))
        {
            GasCylinderSupplier? supplier = await suppliers.GetByIdAsync(group.Key, cancellationToken);

            lines.Add(new SupplierComparisonReportLineDto(
                group.Key.Value, supplier?.Name ?? "(unknown)", group.Count(), group.Sum(p => p.Quantity),
                group.Sum(p => p.GrandTotal), Math.Round(group.Average(p => p.UnitPricePerKg), 2)));
        }

        return lines;
    }
}
