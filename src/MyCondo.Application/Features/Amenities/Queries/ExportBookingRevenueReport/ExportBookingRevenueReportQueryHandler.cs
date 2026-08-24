using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetBookingRevenueReport;

namespace MyCondo.Application.Features.Amenities.Queries.ExportBookingRevenueReport;

/// <summary>Reuses <see cref="GetBookingRevenueReportQuery"/> for the actual report data (tenant
/// scoping, per-facility revenue aggregation) rather than duplicating that logic — this handler's only
/// job is mapping the resulting DTOs to the format-agnostic <see cref="ReportExportDocument"/> and
/// handing it to the centralized <see cref="IReportExportService"/>.</summary>
public sealed class ExportBookingRevenueReportQueryHandler(
    ISender sender,
    IReportExportService exportService
) : IRequestHandler<ExportBookingRevenueReportQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportBookingRevenueReportQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<BookingRevenueReportLineDto> lines = await sender.Send(
            new GetBookingRevenueReportQuery(query.FromDate, query.ToDate), cancellationToken);

        ReportExportDocument document = BookingRevenueExportMapper.ToExportDocument(lines, query.FromDate, query.ToDate);

        string fileName = $"booking-revenue-{query.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
