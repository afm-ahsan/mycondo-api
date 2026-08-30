using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;

namespace MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;

/// <summary>Reuses <see cref="GetPlatformBillingSummaryQuery"/> for the actual data (currency grouping,
/// invoiced/collected/outstanding/overdue calculation) rather than duplicating it — this handler's only
/// job is mapping the resulting rows to the format-agnostic <see cref="ReportExportDocument"/> and
/// handing it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportPlatformBillingSummaryQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportPlatformBillingSummaryQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(
        ExportPlatformBillingSummaryQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformBillingSummaryCurrencyDto> summary = await sender.Send(
            new GetPlatformBillingSummaryQuery(query.OrganizationId, query.DateFrom, query.DateTo), cancellationToken);
        ReportExportDocument document = PlatformBillingSummaryExportMapper.ToExportDocument(
            summary, query.DateFrom, query.DateTo);

        string fileName = $"platform-billing-summary-{DateTime.UtcNow:yyyy-MM-dd}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
