using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetCashBankPositionReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportCashBankPositionReport;

/// <summary>Reuses <see cref="GetCashBankPositionReportQuery"/> for the actual report data (tenant
/// scoping, per-FinancialAccount balances) rather than duplicating that logic — this handler's only job
/// is mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it
/// to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportCashBankPositionReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportCashBankPositionReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportCashBankPositionReportQuery query, CancellationToken cancellationToken)
    {
        CashBankPositionReportDto report = await sender.Send(
            new GetCashBankPositionReportQuery(query.AsOfDate), cancellationToken);
        ReportExportDocument document = CashBankPositionExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"cash-bank-position-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
