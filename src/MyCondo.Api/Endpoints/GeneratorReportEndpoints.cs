using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.ExportGeneratorMaintenanceDueReport;
using MyCondo.Application.Features.Operations.Queries.ExportGeneratorOperationalReport;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceDueReport;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorOperationalReport;

namespace MyCondo.Api.Endpoints;

public static class GeneratorReportEndpoints
{
    public static IEndpointRouteBuilder MapGeneratorReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/operations/generators").WithTags("Generator Reports");

        reports.MapGet("/operational", async (Guid? generatorId, DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<GeneratorOperationalReportLineDto> result = await sender.Send(
                    new GetGeneratorOperationalReportQuery(generatorId, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.report")
            .Produces<IReadOnlyList<GeneratorOperationalReportLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/operational/export", async (
                Guid? generatorId, DateOnly fromDate, DateOnly toDate, string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(
                    new ExportGeneratorOperationalReportQuery(generatorId, fromDate, toDate, parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("generator.report")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        reports.MapGet("/maintenance-due", async (ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<GeneratorMaintenanceDueReportLineDto> result = await sender.Send(
                    new GetGeneratorMaintenanceDueReportQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.report")
            .Produces<IReadOnlyList<GeneratorMaintenanceDueReportLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/maintenance-due/export", async (string format, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse(format, ignoreCase: true, out ReportExportFormat parsedFormat))
                {
                    return Results.BadRequest(new { error = "format must be 'csv' or 'pdf'." });
                }

                ReportExportResult result = await sender.Send(new ExportGeneratorMaintenanceDueReportQuery(parsedFormat), ct);
                return Results.File(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission("generator.report")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}
