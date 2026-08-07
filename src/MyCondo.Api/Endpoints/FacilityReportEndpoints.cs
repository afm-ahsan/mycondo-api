using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Amenities.DTOs;
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

        reports.MapGet("/booking-revenue", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<BookingRevenueReportLineDto> result = await sender.Send(
                    new GetBookingRevenueReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.facility")
            .Produces<IReadOnlyList<BookingRevenueReportLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/pool-daily-usage", async (Guid facilityId, DateOnly date, ISender sender, CancellationToken ct) =>
            {
                PoolDailyUsageReportDto result = await sender.Send(new GetPoolDailyUsageReportQuery(facilityId, date), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.facility")
            .Produces<PoolDailyUsageReportDto>(StatusCodes.Status200OK);

        return app;
    }
}
