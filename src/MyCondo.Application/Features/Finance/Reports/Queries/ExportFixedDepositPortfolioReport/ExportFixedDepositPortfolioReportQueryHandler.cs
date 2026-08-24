using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;

/// <summary>Reuses <see cref="GetFixedDepositPortfolioReportQuery"/> for the actual report data (tenant
/// scoping, instrument/accrual composition) rather than duplicating that logic — this handler's only
/// job is mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and
/// handing it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFixedDepositPortfolioReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFixedDepositPortfolioReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFixedDepositPortfolioReportQuery query, CancellationToken cancellationToken)
    {
        FixedDepositPortfolioReportDto report = await sender.Send(
            new GetFixedDepositPortfolioReportQuery(query.AsOfDate), cancellationToken);
        ReportExportDocument document = FixedDepositPortfolioExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"fixed-deposit-portfolio-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
