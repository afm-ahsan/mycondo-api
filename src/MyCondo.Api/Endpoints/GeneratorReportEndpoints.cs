using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.DTOs;
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

        reports.MapGet("/maintenance-due", async (ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<GeneratorMaintenanceDueReportLineDto> result = await sender.Send(
                    new GetGeneratorMaintenanceDueReportQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.report")
            .Produces<IReadOnlyList<GeneratorMaintenanceDueReportLineDto>>(StatusCodes.Status200OK);

        return app;
    }
}
