using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;

namespace MyCondo.Application.Features.Payments.Queries.ExportFinancialSummaryReport;

/// <summary>Reuses <see cref="GetFinancialSummaryReportQuery"/> for the actual report data (tenant
/// scoping, aggregation) rather than duplicating that logic — this handler's only job is mapping the
/// resulting DTO to the format-agnostic <see cref="ReportExportDocument"/> and handing it to the
/// centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportFinancialSummaryReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportFinancialSummaryReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFinancialSummaryReportQuery query, CancellationToken cancellationToken)
    {
        FinancialSummaryDto report = await sender.Send(
            new GetFinancialSummaryReportQuery(query.BuildingId, query.FromDate, query.ToDate), cancellationToken);

        // Metadata should show the building's name, not its raw GUID — resolve it via the existing
        // building lookup rather than duplicating property data on this DTO.
        string? buildingName = query.BuildingId is Guid buildingId
            ? (await sender.Send(new GetBuildingByIdQuery(buildingId), cancellationToken)).Name
            : null;
        ReportExportDocument document = FinancialSummaryExportMapper.ToExportDocument(report, buildingName);

        string fileName =
            $"financial-summary-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
