using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorOperationalReport;

namespace MyCondo.Application.Features.Operations.Queries.ExportGeneratorOperationalReport;

/// <summary>Reuses <see cref="GetGeneratorOperationalReportQuery"/> for the actual report data (tenant
/// scoping, aggregation) rather than duplicating that logic — this handler's only job is mapping the
/// resulting lines to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportGeneratorOperationalReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportGeneratorOperationalReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportGeneratorOperationalReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<GeneratorOperationalReportLineDto> lines = await sender.Send(
            new GetGeneratorOperationalReportQuery(query.GeneratorId, query.FromDate, query.ToDate), cancellationToken);
        ReportExportDocument document = GeneratorOperationalExportMapper.ToExportDocument(lines, query.FromDate, query.ToDate);

        string fileName = $"generator-operational-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
