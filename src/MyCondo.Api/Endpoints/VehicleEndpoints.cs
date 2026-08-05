using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForVehicle;
using MyCondo.Application.Features.Security.Vehicles.Commands.BlockVehicle;
using MyCondo.Application.Features.Security.Vehicles.Commands.RegisterVehicle;
using MyCondo.Application.Features.Security.Vehicles.Commands.UnblockVehicle;
using MyCondo.Application.Features.Security.Vehicles.DTOs;
using MyCondo.Application.Features.Security.Vehicles.Queries.GetVehicleByRegistrationNumber;
using MyCondo.Application.Features.Security.Vehicles.Queries.GetVehiclesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder vehicles = app.MapGroup("/api/v1/vehicles").WithTags("Vehicles");

        vehicles.MapPost("/", async (RegisterVehicleCommand command, ISender sender, CancellationToken ct) =>
            {
                VehicleDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("vehicle.create")
            .Produces<VehicleDto>(StatusCodes.Status200OK);

        vehicles.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<VehicleDto> result = await sender.Send(
                    new GetVehiclesForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("vehicle.view")
            .Produces<PagedResult<VehicleDto>>(StatusCodes.Status200OK);

        vehicles.MapGet("/by-registration/{registrationNumber}", async (string registrationNumber, ISender sender, CancellationToken ct) =>
            {
                VehicleDto? result = await sender.Send(new GetVehicleByRegistrationNumberQuery(registrationNumber), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequirePermission("vehicle.view")
            .Produces<VehicleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        vehicles.MapPost("/{id:guid}/block", async (Guid id, BlockVehicleRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new BlockVehicleCommand(id, body.Reason), ct);
                return Results.NoContent();
            })
            .RequirePermission("vehicle.block.manage")
            .Produces(StatusCodes.Status204NoContent);

        vehicles.MapPost("/{id:guid}/unblock", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UnblockVehicleCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("vehicle.block.manage")
            .Produces(StatusCodes.Status204NoContent);

        vehicles.MapGet("/{id:guid}/trips", async (Guid id, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<AccessSessionDto> result = await sender.Send(
                    new GetAccessSessionsForVehicleQuery(id, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("vehicle.view")
            .Produces<PagedResult<AccessSessionDto>>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record BlockVehicleRequest(string Reason);
