using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetSupplierComparisonReport;

namespace MyCondo.Application.Features.Operations.Queries.ExportSupplierComparisonReport;

/// <summary>Reuses <see cref="GetSupplierComparisonReportQuery"/> for the actual report data (tenant
/// scoping, aggregation) rather than duplicating that logic — this handler's only job is mapping the
/// resulting lines to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportSupplierComparisonReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportSupplierComparisonReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportSupplierComparisonReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<SupplierComparisonReportLineDto> lines = await sender.Send(
            new GetSupplierComparisonReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = SupplierComparisonExportMapper.ToExportDocument(lines, query.FromDate, query.ToDate);

        string fileName = $"supplier-comparison-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
