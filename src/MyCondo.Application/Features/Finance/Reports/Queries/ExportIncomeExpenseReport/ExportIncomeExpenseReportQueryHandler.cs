using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportIncomeExpenseReport;

/// <summary>Reuses <see cref="GetIncomeExpenseReportQuery"/> for the actual report data (tenant scoping,
/// category activity) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportIncomeExpenseReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportIncomeExpenseReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportIncomeExpenseReportQuery query, CancellationToken cancellationToken)
    {
        IncomeExpenseReportDto report = await sender.Send(
            new GetIncomeExpenseReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = IncomeExpenseExportMapper.ToExportDocument(report);

        string fileName = $"income-expense-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
