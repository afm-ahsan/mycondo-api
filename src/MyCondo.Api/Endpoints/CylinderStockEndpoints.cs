using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.CreateMonthlyReconciliation;
using MyCondo.Application.Features.Operations.Commands.RecordStockAdjustment;
using MyCondo.Application.Features.Operations.Commands.RecordStockMovement;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetCurrentStock;
using MyCondo.Application.Features.Operations.Queries.GetMonthlyReconciliations;
using MyCondo.Application.Features.Operations.Queries.GetStockMovements;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class CylinderStockEndpoints
{
    public static IEndpointRouteBuilder MapCylinderStockEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder movements = app.MapGroup("/api/v1/cylinder-stock-movements").WithTags("Cylinder Stock");

        movements.MapPost("/", async (RecordStockMovementCommand command, ISender sender, CancellationToken ct) =>
            {
                CylinderStockMovementDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.stock.manage")
            .Produces<CylinderStockMovementDto>(StatusCodes.Status200OK);

        movements.MapPost("/adjustments", async (RecordStockAdjustmentCommand command, ISender sender, CancellationToken ct) =>
            {
                CylinderStockMovementDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.approve")
            .Produces<CylinderStockMovementDto>(StatusCodes.Status200OK);

        movements.MapGet("/", async (string? cylinderType, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<CylinderStockMovementDto> result = await sender.Send(
                    new GetStockMovementsQuery(cylinderType, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<PagedResult<CylinderStockMovementDto>>(StatusCodes.Status200OK);

        movements.MapGet("/current", async (string? cylinderType, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<CylinderStockDto> result = await sender.Send(new GetCurrentStockQuery(cylinderType), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<IReadOnlyList<CylinderStockDto>>(StatusCodes.Status200OK);

        RouteGroupBuilder reconciliations = app.MapGroup("/api/v1/cylinder-reconciliations").WithTags("Cylinder Stock");

        reconciliations.MapPost("/", async (CreateMonthlyReconciliationCommand command, ISender sender, CancellationToken ct) =>
            {
                MonthlyCylinderReconciliationDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.stock.manage")
            .Produces<MonthlyCylinderReconciliationDto>(StatusCodes.Status200OK);

        reconciliations.MapGet("/", async (string? cylinderType, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<MonthlyCylinderReconciliationDto> result = await sender.Send(
                    new GetMonthlyReconciliationsQuery(cylinderType, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<PagedResult<MonthlyCylinderReconciliationDto>>(StatusCodes.Status200OK);

        return app;
    }
}
