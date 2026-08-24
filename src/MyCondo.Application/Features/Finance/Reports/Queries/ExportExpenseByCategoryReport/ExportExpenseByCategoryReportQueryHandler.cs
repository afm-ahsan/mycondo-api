using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportExpenseByCategoryReport;

/// <summary>Reuses <see cref="GetExpenseByCategoryReportQuery"/> for the actual report data (tenant scoping,
/// composition logic) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportExpenseByCategoryReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportExpenseByCategoryReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportExpenseByCategoryReportQuery query, CancellationToken cancellationToken)
    {
        ExpenseByCategoryReportDto report = await sender.Send(
            new GetExpenseByCategoryReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = ExpenseByCategoryExportMapper.ToExportDocument(report);

        string fileName = $"expense-by-category-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
