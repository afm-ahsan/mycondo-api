using System.Globalization;
using System.Text;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetPoolDailyUsageReport;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.ExportPoolDailyUsageReport;

/// <summary>Reuses <see cref="GetPoolDailyUsageReportQuery"/> for the actual report data (tenant
/// scoping, entry/exit sweep for peak occupancy) rather than duplicating that logic — this handler's
/// only job is resolving the facility's name (the underlying DTO carries no facility identity at all)
/// and mapping the result to the format-agnostic <see cref="ReportExportDocument"/> for the centralized
/// <see cref="IReportExportService"/>.</summary>
public sealed class ExportPoolDailyUsageReportQueryHandler(
    ISender sender,
    IFacilityRepository facilities,
    IReportExportService exportService
) : IRequestHandler<ExportPoolDailyUsageReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportPoolDailyUsageReportQuery query, CancellationToken cancellationToken)
    {
        PoolDailyUsageReportDto report = await sender.Send(
            new GetPoolDailyUsageReportQuery(query.FacilityId, query.Date), cancellationToken);

        Facility? facility = await facilities.GetByIdAsync(new FacilityId(query.FacilityId), cancellationToken);
        string facilityName = facility?.Name ?? "(unknown)";

        ReportExportDocument document = PoolDailyUsageExportMapper.ToExportDocument(report, facilityName);

        string fileName = $"pool-daily-usage-{SanitizeForFileName(facilityName)}" +
            $"-{query.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }

    private static string SanitizeForFileName(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        return builder.ToString();
    }
}
