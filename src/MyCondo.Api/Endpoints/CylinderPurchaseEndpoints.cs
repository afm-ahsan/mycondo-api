using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.ApproveCylinderPurchase;
using MyCondo.Application.Features.Operations.Commands.MarkCylinderPurchasePaid;
using MyCondo.Application.Features.Operations.Commands.RecordCylinderPurchase;
using MyCondo.Application.Features.Operations.Commands.RejectCylinderPurchase;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetCylinderPurchaseById;
using MyCondo.Application.Features.Operations.Queries.GetCylinderPurchases;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class CylinderPurchaseEndpoints
{
    public static IEndpointRouteBuilder MapCylinderPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder purchases = app.MapGroup("/api/v1/cylinder-purchases").WithTags("Cylinder Purchases");

        purchases.MapPost("/", async (RecordCylinderPurchaseCommand command, ISender sender, CancellationToken ct) =>
            {
                CylinderPurchaseDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.purchase.manage")
            .Produces<CylinderPurchaseDto>(StatusCodes.Status200OK);

        purchases.MapGet("/", async (Guid? supplierId, string? approvalStatus, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<CylinderPurchaseDto> result = await sender.Send(
                    new GetCylinderPurchasesQuery(supplierId, approvalStatus, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<PagedResult<CylinderPurchaseDto>>(StatusCodes.Status200OK);

        purchases.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                CylinderPurchaseDto result = await sender.Send(new GetCylinderPurchaseByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<CylinderPurchaseDto>(StatusCodes.Status200OK);

        purchases.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                CylinderPurchaseDto result = await sender.Send(new ApproveCylinderPurchaseCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.approve")
            .Produces<CylinderPurchaseDto>(StatusCodes.Status200OK);

        purchases.MapPost("/{id:guid}/reject", async (Guid id, RejectCylinderPurchaseRequest body, ISender sender, CancellationToken ct) =>
            {
                CylinderPurchaseDto result = await sender.Send(new RejectCylinderPurchaseCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.approve")
            .Produces<CylinderPurchaseDto>(StatusCodes.Status200OK);

        purchases.MapPost("/{id:guid}/mark-paid", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                CylinderPurchaseDto result = await sender.Send(new MarkCylinderPurchasePaidCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.purchase.manage")
            .Produces<CylinderPurchaseDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record RejectCylinderPurchaseRequest(string Reason);
