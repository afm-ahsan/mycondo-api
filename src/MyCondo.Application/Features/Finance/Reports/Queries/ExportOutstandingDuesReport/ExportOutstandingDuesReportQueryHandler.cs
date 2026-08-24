using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportOutstandingDuesReport;

/// <summary>Reuses <see cref="GetOutstandingDuesReportQuery"/> for the actual report data (tenant
/// scoping, per-flat receivable aggregation) rather than duplicating that logic — this handler's only
/// job is mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and
/// handing it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportOutstandingDuesReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportOutstandingDuesReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportOutstandingDuesReportQuery query, CancellationToken cancellationToken)
    {
        OutstandingDuesReportDto report = await sender.Send(
            new GetOutstandingDuesReportQuery(query.BuildingId), cancellationToken);
        ReportExportDocument document = OutstandingDuesExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"outstanding-dues-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
