using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportGasCollectionReport;

/// <summary>Reuses <see cref="GetGasCollectionReportQuery"/> for the actual report data (tenant
/// scoping, Billed/Collected/Waived aggregation) rather than duplicating that logic — this handler's
/// only job is mapping the resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and
/// handing it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportGasCollectionReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportGasCollectionReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportGasCollectionReportQuery query, CancellationToken cancellationToken)
    {
        GasCollectionReportDto report = await sender.Send(
            new GetGasCollectionReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = GasCollectionExportMapper.ToExportDocument(report);

        string fileName = $"gas-collection-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
