using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFundPosition;

/// <summary>Reuses <see cref="GetFundPositionQuery"/> for the actual report data (tenant scoping, fund
/// activity) rather than duplicating that logic — this handler's only job is mapping the resulting DTO
/// to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the centralized
/// <see cref="IReportExportService"/>.</summary>
public sealed class ExportFundPositionQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFundPositionQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFundPositionQuery query, CancellationToken cancellationToken)
    {
        FundPositionReportDto report = await sender.Send(new GetFundPositionQuery(query.AsOfDate), cancellationToken);
        ReportExportDocument document = FundPositionExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"fund-position-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
