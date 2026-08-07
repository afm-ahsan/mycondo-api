using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetCylinderConsumptionReport;
using MyCondo.Application.Features.Operations.Queries.GetSupplierComparisonReport;

namespace MyCondo.Api.Endpoints;

public static class GasCylinderReportEndpoints
{
    public static IEndpointRouteBuilder MapGasCylinderReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder reports = app.MapGroup("/api/v1/reports/operations/gas-cylinders").WithTags("Gas Cylinder Reports");

        reports.MapGet("/supplier-comparison", async (DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<SupplierComparisonReportLineDto> result = await sender.Send(
                    new GetSupplierComparisonReportQuery(fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.report")
            .Produces<IReadOnlyList<SupplierComparisonReportLineDto>>(StatusCodes.Status200OK);

        reports.MapGet("/consumption", async (string? cylinderType, DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<CylinderConsumptionReportLineDto> result = await sender.Send(
                    new GetCylinderConsumptionReportQuery(cylinderType, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.report")
            .Produces<IReadOnlyList<CylinderConsumptionReportLineDto>>(StatusCodes.Status200OK);

        return app;
    }
}
