using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;
using MyCondo.Application.Features.Utilities.Queries.GetMeterStatusSummaryReport;
using MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;

namespace MyCondo.Api.Endpoints;

public static class UtilityReportEndpoints
{
    public static IEndpointRouteBuilder MapUtilityReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/utilities").WithTags("Utility Reports");

        reports.MapGet("/consumption-summary", async (
                Guid? buildingId, string? utilityType, DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<ConsumptionSummaryLineDto> result = await sender.Send(
                    new GetConsumptionSummaryReportQuery(buildingId, utilityType, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.report")
            .Produces<IReadOnlyList<ConsumptionSummaryLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/reading-status-summary", async (Guid? buildingId, string? utilityType, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<ReadingStatusSummaryLineDto> result = await sender.Send(
                    new GetReadingStatusSummaryReportQuery(buildingId, utilityType), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.report")
            .Produces<IReadOnlyList<ReadingStatusSummaryLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/meter-status-summary", async (Guid? buildingId, string? utilityType, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<MeterStatusSummaryLineDto> result = await sender.Send(
                    new GetMeterStatusSummaryReportQuery(buildingId, utilityType), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.report")
            .Produces<IReadOnlyList<MeterStatusSummaryLineDto>>(StatusCodes.Status200OK);

        return app;
    }
}
