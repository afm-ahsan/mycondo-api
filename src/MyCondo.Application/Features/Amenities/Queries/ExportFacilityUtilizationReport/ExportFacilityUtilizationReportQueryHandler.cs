using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetFacilityUtilizationReport;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.ExportFacilityUtilizationReport;

/// <summary>Reuses <see cref="GetFacilityUtilizationReportQuery"/> for the actual report data (tenant
/// scoping, per-facility aggregation) rather than duplicating that logic — this handler's only job is
/// mapping the resulting DTOs to the format-agnostic <see cref="ReportExportDocument"/> and handing it
/// to the centralized <see cref="IReportExportService"/>. When scoped to a single facility, the
/// facility's name is resolved separately for the export's metadata line since the underlying list can
/// come back empty (no bookings in the period) with no line to read the name from.</summary>
public sealed class ExportFacilityUtilizationReportQueryHandler(
    ISender sender,
    IFacilityRepository facilities,
    IReportExportService exportService
) : IRequestHandler<ExportFacilityUtilizationReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFacilityUtilizationReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<FacilityUtilizationReportLineDto> lines = await sender.Send(
            new GetFacilityUtilizationReportQuery(query.FacilityId, query.FromDate, query.ToDate), cancellationToken);

        string? facilityScope = null;
        if (query.FacilityId is Guid rawFacilityId)
        {
            Facility? facility = await facilities.GetByIdAsync(new FacilityId(rawFacilityId), cancellationToken);
            facilityScope = facility?.Name ?? "(unknown)";
        }

        ReportExportDocument document = FacilityUtilizationExportMapper.ToExportDocument(
            lines, query.FromDate, query.ToDate, facilityScope);

        string fileName = $"facility-utilization-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
