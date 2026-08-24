using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;

namespace MyCondo.Application.Features.Utilities.Queries.ExportReadingStatusSummaryReport;

/// <summary>Reuses <see cref="GetReadingStatusSummaryReportQuery"/> for the actual report data (tenant
/// scoping, status aggregation) rather than duplicating that logic — this handler's only job is
/// mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it
/// to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportReadingStatusSummaryReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportReadingStatusSummaryReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportReadingStatusSummaryReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReadingStatusSummaryLineDto> report = await sender.Send(
            new GetReadingStatusSummaryReportQuery(query.BuildingId, query.UtilityType), cancellationToken);

        ReportExportDocument document = ReadingStatusSummaryExportMapper.ToExportDocument(
            report, query.BuildingId, query.UtilityType);

        DateOnly asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"reading-status-summary-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
