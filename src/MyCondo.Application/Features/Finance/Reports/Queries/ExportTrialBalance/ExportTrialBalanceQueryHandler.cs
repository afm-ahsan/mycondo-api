using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportTrialBalance;

/// <summary>Reuses <see cref="GetTrialBalanceQuery"/> for the actual report data (tenant scoping,
/// netting, metadata) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportTrialBalanceQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportTrialBalanceQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportTrialBalanceQuery query, CancellationToken cancellationToken)
    {
        TrialBalanceReportDto report = await sender.Send(new GetTrialBalanceQuery(query.AsOfDate), cancellationToken);
        ReportExportDocument document = TrialBalanceExportMapper.ToExportDocument(report);

        DateOnly asOfDate = report.Metadata.AsOfDate ?? query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        string fileName = $"trial-balance-{asOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
