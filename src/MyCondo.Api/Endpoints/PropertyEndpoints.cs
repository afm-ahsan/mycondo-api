using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingsForTenant;
using MyCondo.Application.Features.Property.Flats.Commands.CreateFlat;
using MyCondo.Application.Features.Property.Flats.Commands.UpdateFlatArea;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForBuilding;
using MyCondo.Application.Features.Property.Gates.Commands.CreateGate;
using MyCondo.Application.Features.Property.Gates.DTOs;
using MyCondo.Application.Features.Property.Gates.Queries.GetGatesForBuilding;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class PropertyEndpoints
{
    public static IEndpointRouteBuilder MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder buildings = app.MapGroup("/api/v1/properties/buildings").WithTags("Property");

        buildings.MapPost("/", async (CreateBuildingCommand command, ISender sender, CancellationToken ct) =>
            {
                CreateBuildingResult result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.create")
            .Produces<CreateBuildingResult>(StatusCodes.Status200OK);

        buildings.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<BuildingDto> result = await sender.Send(
                    new GetBuildingsForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.view")
            .Produces<PagedResult<BuildingDto>>(StatusCodes.Status200OK);

        buildings.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                BuildingDto result = await sender.Send(new GetBuildingByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.view")
            .Produces<BuildingDto>(StatusCodes.Status200OK);

        buildings.MapPost("/{buildingId:guid}/flats", async (Guid buildingId, CreateFlatRequest body, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(
                    new CreateFlatCommand(buildingId, body.FlatNumber, body.FloorNumber, body.FlatType), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.create")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapGet("/{buildingId:guid}/flats", async (Guid buildingId, string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<FlatDto> result = await sender.Send(
                    new GetFlatsForBuildingQuery(buildingId, search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.view")
            .Produces<PagedResult<FlatDto>>(StatusCodes.Status200OK);

        buildings.MapPatch("/{buildingId:guid}/flats/{flatId:guid}/area", async (Guid buildingId, Guid flatId, UpdateFlatAreaRequest body, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(new UpdateFlatAreaCommand(flatId, body.AreaSqFt), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.update")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapPost("/{buildingId:guid}/gates", async (Guid buildingId, CreateGateRequest body, ISender sender, CancellationToken ct) =>
            {
                GateDto result = await sender.Send(new CreateGateCommand(buildingId, body.Name), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.create")
            .Produces<GateDto>(StatusCodes.Status200OK);

        buildings.MapGet("/{buildingId:guid}/gates", async (Guid buildingId, ISender sender, CancellationToken ct) =>
            {
                List<GateDto> result = await sender.Send(new GetGatesForBuildingQuery(buildingId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.view")
            .Produces<List<GateDto>>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CreateFlatRequest(string FlatNumber, int? FloorNumber, string FlatType);

public sealed record UpdateFlatAreaRequest(decimal? AreaSqFt);

public sealed record CreateGateRequest(string Name);
