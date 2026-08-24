using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFineReport;

/// <summary>Reuses <see cref="GetFineReportQuery"/> for the actual report data (tenant scoping,
/// Billed/Collected/Waived aggregation) rather than duplicating that logic — this handler's only job is
/// mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to
/// the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFineReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFineReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFineReportQuery query, CancellationToken cancellationToken)
    {
        FineReportDto report = await sender.Send(
            new GetFineReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = FineReportExportMapper.ToExportDocument(report);

        string fileName = $"fines-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
