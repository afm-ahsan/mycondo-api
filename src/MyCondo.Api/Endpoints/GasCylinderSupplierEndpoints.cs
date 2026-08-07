using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.CreateSupplier;
using MyCondo.Application.Features.Operations.Commands.DeactivateSupplier;
using MyCondo.Application.Features.Operations.Commands.ReactivateSupplier;
using MyCondo.Application.Features.Operations.Commands.UpdateSupplier;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetSupplierById;
using MyCondo.Application.Features.Operations.Queries.GetSuppliers;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class GasCylinderSupplierEndpoints
{
    public static IEndpointRouteBuilder MapGasCylinderSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder suppliers = app.MapGroup("/api/v1/gas-cylinder-suppliers").WithTags("Gas Cylinder Suppliers");

        suppliers.MapPost("/", async (CreateSupplierCommand command, ISender sender, CancellationToken ct) =>
            {
                GasCylinderSupplierDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.purchase.manage")
            .Produces<GasCylinderSupplierDto>(StatusCodes.Status200OK);

        suppliers.MapGet("/", async (int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GasCylinderSupplierDto> result = await sender.Send(
                    new GetSuppliersQuery(page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<PagedResult<GasCylinderSupplierDto>>(StatusCodes.Status200OK);

        suppliers.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                GasCylinderSupplierDto result = await sender.Send(new GetSupplierByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.view")
            .Produces<GasCylinderSupplierDto>(StatusCodes.Status200OK);

        suppliers.MapPut("/{id:guid}", async (Guid id, UpdateSupplierRequest body, ISender sender, CancellationToken ct) =>
            {
                GasCylinderSupplierDto result = await sender.Send(
                    new UpdateSupplierCommand(id, body.Name, body.ContactPhone, body.ContactEmail, body.Address), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.purchase.manage")
            .Produces<GasCylinderSupplierDto>(StatusCodes.Status200OK);

        suppliers.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                GasCylinderSupplierDto result = await sender.Send(new DeactivateSupplierCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.purchase.manage")
            .Produces<GasCylinderSupplierDto>(StatusCodes.Status200OK);

        suppliers.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                GasCylinderSupplierDto result = await sender.Send(new ReactivateSupplierCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("gascylinder.purchase.manage")
            .Produces<GasCylinderSupplierDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record UpdateSupplierRequest(string Name, string? ContactPhone, string? ContactEmail, string? Address);
