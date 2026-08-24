using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportServiceChargeCollectionReport;

/// <summary>Reuses <see cref="GetServiceChargeCollectionReportQuery"/> for the actual report data
/// (tenant scoping, Billed/Collected/Waived aggregation) rather than duplicating that logic — this
/// handler's only job is mapping the resulting DTO to the format-agnostic
/// <see cref="ReportExportDocument"/> and handing it to the centralized
/// <see cref="IReportExportService"/>.</summary>
public sealed class ExportServiceChargeCollectionReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportServiceChargeCollectionReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(
        ExportServiceChargeCollectionReportQuery query, CancellationToken cancellationToken)
    {
        ServiceChargeCollectionReportDto report = await sender.Send(
            new GetServiceChargeCollectionReportQuery(query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = ServiceChargeCollectionExportMapper.ToExportDocument(report);

        string fileName = $"service-charge-collection-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
