using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierComparisonReport;

public sealed record GetSupplierComparisonReportQuery(
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<SupplierComparisonReportLineDto>>;
