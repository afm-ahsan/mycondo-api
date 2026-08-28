using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Operations.Queries.ExportSupplierComparisonReport;

public sealed record ExportSupplierComparisonReportQuery(DateOnly FromDate, DateOnly ToDate, ReportExportFormat Format)
    : IRequest<ReportExportResult>, IRequiresFeature
{
    public string FeatureKey => "operations.gas_cylinders";
}
