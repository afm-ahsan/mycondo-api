using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFinancialPosition;

/// <summary>Reuses <see cref="GetFinancialPositionQuery"/> for the actual report data (tenant scoping,
/// balance-sheet derivation) rather than duplicating that logic — this handler's only job is mapping
/// the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFinancialPositionQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFinancialPositionQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFinancialPositionQuery query, CancellationToken cancellationToken)
    {
        FinancialPositionReportDto report = await sender.Send(new GetFinancialPositionQuery(query.AsOfDate), cancellationToken);
        ReportExportDocument document = FinancialPositionExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"financial-position-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
