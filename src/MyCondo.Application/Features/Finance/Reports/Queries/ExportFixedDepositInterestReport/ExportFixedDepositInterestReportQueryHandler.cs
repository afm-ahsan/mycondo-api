using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositInterestReport;

/// <summary>Reuses <see cref="GetFixedDepositInterestReportQuery"/> for the actual report data (tenant
/// scoping, accrual/receipt reconciliation) rather than duplicating that logic — this handler's only job
/// is mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it
/// to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFixedDepositInterestReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFixedDepositInterestReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFixedDepositInterestReportQuery query, CancellationToken cancellationToken)
    {
        FixedDepositInterestReportDto report = await sender.Send(
            new GetFixedDepositInterestReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = FixedDepositInterestExportMapper.ToExportDocument(report);

        string fileName = $"fixed-deposit-interest-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
