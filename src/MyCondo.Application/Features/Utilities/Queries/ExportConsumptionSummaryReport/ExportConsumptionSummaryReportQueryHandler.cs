using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;

namespace MyCondo.Application.Features.Utilities.Queries.ExportConsumptionSummaryReport;

/// <summary>Reuses <see cref="GetConsumptionSummaryReportQuery"/> for the actual report data (tenant
/// scoping, aggregation) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportConsumptionSummaryReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportConsumptionSummaryReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportConsumptionSummaryReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ConsumptionSummaryLineDto> report = await sender.Send(
            new GetConsumptionSummaryReportQuery(query.BuildingId, query.UtilityType, query.FromDate, query.ToDate),
            cancellationToken);

        ReportExportDocument document = ConsumptionSummaryExportMapper.ToExportDocument(
            report, query.BuildingId, query.UtilityType, query.FromDate, query.ToDate);

        string fileName = $"consumption-summary-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
