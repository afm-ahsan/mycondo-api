using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseByTypeReport;

/// <summary>Reuses <see cref="GetExpenseByTypeReportQuery"/> for the actual report data (tenant scoping,
/// composition logic) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportExpenseByTypeReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportExpenseByTypeReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportExpenseByTypeReportQuery query, CancellationToken cancellationToken)
    {
        ExpenseByTypeReportDto report = await sender.Send(
            new GetExpenseByTypeReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = ExpenseByTypeExportMapper.ToExportDocument(report);

        string fileName = $"expense-by-type-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
