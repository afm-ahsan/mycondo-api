using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseTrendReport;

/// <summary>Reuses <see cref="GetExpenseTrendReportQuery"/> for the actual report data (tenant scoping,
/// ledger activity) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportExpenseTrendReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportExpenseTrendReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportExpenseTrendReportQuery query, CancellationToken cancellationToken)
    {
        ExpenseTrendReportDto report = await sender.Send(
            new GetExpenseTrendReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = ExpenseTrendExportMapper.ToExportDocument(report);

        string fileName = $"expense-trend-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
