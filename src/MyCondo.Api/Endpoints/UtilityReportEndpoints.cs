using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.ExportConsumptionHistoryReport;
using MyCondo.Application.Features.Utilities.Queries.ExportConsumptionSummaryReport;
using MyCondo.Application.Features.Utilities.Queries.ExportReadingStatusSummaryReport;
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

        reports.MapGet("/consumption-summary/export", async (
                Guid? buildingId, string? utilityType, DateOnly fromDate, DateOnly toDate, string format,
                ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportConsumptionSummaryReportQuery(buildingId, utilityType, fromDate, toDate, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("utility.report")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        reports.MapGet("/reading-status-summary", async (Guid? buildingId, string? utilityType, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<ReadingStatusSummaryLineDto> result = await sender.Send(
                    new GetReadingStatusSummaryReportQuery(buildingId, utilityType), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.report")
            .Produces<IReadOnlyList<ReadingStatusSummaryLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/reading-status-summary/export", async (
                Guid? buildingId, string? utilityType, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportReadingStatusSummaryReportQuery(buildingId, utilityType, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("utility.report")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        reports.MapGet("/consumption-history/export", async (
                Guid meterId, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportConsumptionHistoryReportQuery(meterId, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("utility.report")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

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
