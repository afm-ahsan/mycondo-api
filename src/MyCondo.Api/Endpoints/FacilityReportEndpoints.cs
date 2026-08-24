using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.ExportBookingRevenueReport;
using MyCondo.Application.Features.Amenities.Queries.ExportFacilityUtilizationReport;
using MyCondo.Application.Features.Amenities.Queries.ExportPoolDailyUsageReport;
using MyCondo.Application.Features.Amenities.Queries.GetBookingRevenueReport;
using MyCondo.Application.Features.Amenities.Queries.GetFacilityUtilizationReport;
using MyCondo.Application.Features.Amenities.Queries.GetPoolDailyUsageReport;

namespace MyCondo.Api.Endpoints;

public static class FacilityReportEndpoints
{
    public static IEndpointRouteBuilder MapFacilityReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/facilities").WithTags("Facility Reports");

        reports.MapGet("/utilization", async (Guid? facilityId, DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<FacilityUtilizationReportLineDto> result = await sender.Send(
                    new GetFacilityUtilizationReportQuery(facilityId, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.facility")
            .Produces<IReadOnlyList<FacilityUtilizationReportLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/utilization/export", async (
                Guid? facilityId, DateOnly fromDate, DateOnly toDate, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportFacilityUtilizationReportQuery(facilityId, fromDate, toDate, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("report.facility")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        reports.MapGet("/booking-revenue", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<BookingRevenueReportLineDto> result = await sender.Send(
                    new GetBookingRevenueReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.facility")
            .Produces<IReadOnlyList<BookingRevenueReportLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/booking-revenue/export", async (
                DateOnly fromDate, DateOnly toDate, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportBookingRevenueReportQuery(fromDate, toDate, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("report.facility")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        reports.MapGet("/pool-daily-usage", async (Guid facilityId, DateOnly date, ISender sender, CancellationToken ct) =>
            {
                PoolDailyUsageReportDto result = await sender.Send(new GetPoolDailyUsageReportQuery(facilityId, date), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.facility")
            .Produces<PoolDailyUsageReportDto>(StatusCodes.Status200OK);

        reports.MapGet("/pool-daily-usage/export", async (
                Guid facilityId, DateOnly date, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportPoolDailyUsageReportQuery(facilityId, date, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("report.facility")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}
