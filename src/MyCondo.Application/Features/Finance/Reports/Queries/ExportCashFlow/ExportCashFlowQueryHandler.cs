using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashFlow;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportCashFlow;

/// <summary>Reuses <see cref="GetCashFlowQuery"/> for the actual report data (tenant scoping, Operating
/// vs. Investing classification) rather than duplicating that logic — this handler's only job is
/// mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to
/// the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportCashFlowQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportCashFlowQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportCashFlowQuery query, CancellationToken cancellationToken)
    {
        CashFlowReportDto report = await sender.Send(new GetCashFlowQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = CashFlowExportMapper.ToExportDocument(report);

        string fileName = $"cash-flow-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
