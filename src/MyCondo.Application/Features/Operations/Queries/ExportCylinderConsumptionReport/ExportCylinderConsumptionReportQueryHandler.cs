using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetCylinderConsumptionReport;

namespace MyCondo.Application.Features.Operations.Queries.ExportCylinderConsumptionReport;

/// <summary>Reuses <see cref="GetCylinderConsumptionReportQuery"/> for the actual report data (tenant
/// scoping, aggregation) rather than duplicating that logic — this handler's only job is mapping the
/// resulting lines to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportCylinderConsumptionReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportCylinderConsumptionReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportCylinderConsumptionReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CylinderConsumptionReportLineDto> lines = await sender.Send(
            new GetCylinderConsumptionReportQuery(query.CylinderType, query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = CylinderConsumptionExportMapper.ToExportDocument(
            lines, query.CylinderType, query.FromDate, query.ToDate);

        string fileName = $"cylinder-consumption-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
