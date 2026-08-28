using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierComparisonReport;

public sealed record GetSupplierComparisonReportQuery(
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<SupplierComparisonReportLineDto>>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}
