using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseSummaryReport;

/// <summary>Reuses <see cref="GetExpenseSummaryReportQuery"/> for the actual report data (tenant
/// scoping, Ledger-vs-source-record reconciliation) rather than duplicating that logic — this handler's
/// only job is mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and
/// handing it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportExpenseSummaryReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportExpenseSummaryReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportExpenseSummaryReportQuery query, CancellationToken cancellationToken)
    {
        ExpenseSummaryReportDto report = await sender.Send(
            new GetExpenseSummaryReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = ExpenseSummaryExportMapper.ToExportDocument(report);

        string fileName = $"expense-summary-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
