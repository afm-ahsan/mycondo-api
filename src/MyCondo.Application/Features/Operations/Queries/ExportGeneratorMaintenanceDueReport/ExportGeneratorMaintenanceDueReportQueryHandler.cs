using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceDueReport;

namespace MyCondo.Application.Features.Operations.Queries.ExportGeneratorMaintenanceDueReport;

/// <summary>Reuses <see cref="GetGeneratorMaintenanceDueReportQuery"/> for the actual report data
/// (tenant scoping, due-date calculation) rather than duplicating that logic — this handler's only job
/// is mapping the resulting lines to the format-agnostic <see cref="ReportExportDocument"/> and handing
/// it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportGeneratorMaintenanceDueReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportGeneratorMaintenanceDueReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportGeneratorMaintenanceDueReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<GeneratorMaintenanceDueReportLineDto> lines = await sender.Send(
            new GetGeneratorMaintenanceDueReportQuery(), cancellationToken);
        ReportExportDocument document = GeneratorMaintenanceDueExportMapper.ToExportDocument(lines);

        string fileName = $"generator-maintenance-due-{DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
