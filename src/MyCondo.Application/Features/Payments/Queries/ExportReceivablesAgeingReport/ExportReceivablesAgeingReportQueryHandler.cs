using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;

namespace MyCondo.Application.Features.Payments.Queries.ExportReceivablesAgeingReport;

/// <summary>Reuses <see cref="GetReceivablesAgeingReportQuery"/> for the actual report data (tenant
/// scoping, bucketing) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportReceivablesAgeingReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportReceivablesAgeingReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportReceivablesAgeingReportQuery query, CancellationToken cancellationToken)
    {
        ReceivablesAgeingReportDto report = await sender.Send(
            new GetReceivablesAgeingReportQuery(query.BuildingId, query.AsOfDate), cancellationToken);

        // Metadata should show the building's name, not its raw GUID — resolve it via the existing
        // building lookup rather than duplicating property data on this DTO.
        string? buildingName = query.BuildingId is Guid buildingId
            ? (await sender.Send(new GetBuildingByIdQuery(buildingId), cancellationToken)).Name
            : null;
        ReportExportDocument document = ReceivablesAgeingExportMapper.ToExportDocument(report, buildingName);

        string fileName = $"receivables-ageing-{report.AsOfDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
