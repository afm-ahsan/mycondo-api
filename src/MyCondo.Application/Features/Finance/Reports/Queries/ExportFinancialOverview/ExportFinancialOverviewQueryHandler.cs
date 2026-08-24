using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialOverview;

/// <summary>Reuses <see cref="GetFinancialOverviewQuery"/> for the actual report data (tenant scoping,
/// composite dashboard assembly) rather than duplicating that logic — this handler's only job is
/// mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to
/// the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFinancialOverviewQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFinancialOverviewQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFinancialOverviewQuery query, CancellationToken cancellationToken)
    {
        FinancialOverviewReportDto report = await sender.Send(
            new GetFinancialOverviewQuery(query.AsOfDate, query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = FinancialOverviewExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"overview-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
