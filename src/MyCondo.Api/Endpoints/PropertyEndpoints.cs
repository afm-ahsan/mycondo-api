using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;
using MyCondo.Application.Features.Property.Buildings.Commands.UpdateBuilding;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingsForTenant;
using MyCondo.Application.Features.Property.Flats.Commands.CreateFlat;
using MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;
using MyCondo.Application.Features.Property.Flats.Commands.UpdateFlat;
using MyCondo.Application.Features.Property.Flats.Commands.UpdateFlatArea;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Property.Buildings.Commands.SetBuildingPrimaryPhoto;
using MyCondo.Application.Features.Property.Flats.Commands.SetFlatPrimaryPhoto;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatById;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForBuilding;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForTenant;
using MyCondo.Application.Features.Property.Gates.Commands.ActivateGate;
using MyCondo.Application.Features.Property.Gates.Commands.CreateGate;
using MyCondo.Application.Features.Property.Gates.Commands.DeactivateGate;
using MyCondo.Application.Features.Property.Gates.Commands.UpdateGate;
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
            .RequireBuildingPermission("property.view", "id")
            .Produces<BuildingDto>(StatusCodes.Status200OK);

        buildings.MapPut("/{id:guid}", async (Guid id, UpdateBuildingRequest body, ISender sender, CancellationToken ct) =>
            {
                BuildingDto result = await sender.Send(new UpdateBuildingCommand(id, body.Name, body.Code, body.Address), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.update", "id")
            .Produces<BuildingDto>(StatusCodes.Status200OK);

        buildings.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateBuildingCommand(id), ct);
                return Results.NoContent();
            })
            .RequireBuildingPermission("property.delete", "id")
            .Produces(StatusCodes.Status204NoContent);

        buildings.MapPut("/{id:guid}/primary-photo", async (Guid id, SetPrimaryPhotoRequest body, ISender sender, CancellationToken ct) =>
            {
                BuildingDto result = await sender.Send(new SetBuildingPrimaryPhotoCommand(id, body.AttachmentId), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.update", "id")
            .Produces<BuildingDto>(StatusCodes.Status200OK);

        RouteGroupBuilder flats = app.MapGroup("/api/v1/properties/flats").WithTags("Property");

        flats.MapGet("/", async (string? search, Guid? buildingId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<FlatDto> result = await sender.Send(
                    new GetFlatsForTenantQuery(search, buildingId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("property.view")
            .Produces<PagedResult<FlatDto>>(StatusCodes.Status200OK);

        buildings.MapPost("/{buildingId:guid}/flats", async (Guid buildingId, CreateFlatRequest body, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(
                    new CreateFlatCommand(buildingId, body.FlatNumber, body.FloorNumber, body.FlatType), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.create")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapGet("/{buildingId:guid}/flats", async (Guid buildingId, string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<FlatDto> result = await sender.Send(
                    new GetFlatsForBuildingQuery(buildingId, search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.view")
            .Produces<PagedResult<FlatDto>>(StatusCodes.Status200OK);

        buildings.MapGet("/{buildingId:guid}/flats/{flatId:guid}", async (Guid buildingId, Guid flatId, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(new GetFlatByIdQuery(flatId), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.view")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapPut("/{buildingId:guid}/flats/{flatId:guid}", async (Guid buildingId, Guid flatId, UpdateFlatRequest body, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(new UpdateFlatCommand(flatId, body.FlatNumber, body.FloorNumber, body.FlatType), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.update")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapDelete("/{buildingId:guid}/flats/{flatId:guid}", async (Guid buildingId, Guid flatId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateFlatCommand(flatId), ct);
                return Results.NoContent();
            })
            .RequireBuildingPermission("property.delete")
            .Produces(StatusCodes.Status204NoContent);

        buildings.MapPatch("/{buildingId:guid}/flats/{flatId:guid}/area", async (Guid buildingId, Guid flatId, UpdateFlatAreaRequest body, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(new UpdateFlatAreaCommand(flatId, body.AreaSqFt), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.update")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapPut("/{buildingId:guid}/flats/{flatId:guid}/primary-photo", async (Guid buildingId, Guid flatId, SetPrimaryPhotoRequest body, ISender sender, CancellationToken ct) =>
            {
                FlatDto result = await sender.Send(new SetFlatPrimaryPhotoCommand(flatId, body.AttachmentId), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("property.update")
            .Produces<FlatDto>(StatusCodes.Status200OK);

        buildings.MapPost("/{buildingId:guid}/gates", async (Guid buildingId, CreateGateRequest body, ISender sender, CancellationToken ct) =>
            {
                GateDto result = await sender.Send(
                    new CreateGateCommand(
                        buildingId, body.Name, body.Code, body.Description, body.IsEntryAllowed, body.IsExitAllowed,
                        body.DisplayOrder),
                    ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("gate.manage")
            .Produces<GateDto>(StatusCodes.Status200OK);

        buildings.MapGet("/{buildingId:guid}/gates", async (Guid buildingId, bool activeOnly, ISender sender, CancellationToken ct) =>
            {
                List<GateDto> result = await sender.Send(new GetGatesForBuildingQuery(buildingId, activeOnly), ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("gate.view")
            .Produces<List<GateDto>>(StatusCodes.Status200OK);

        buildings.MapPut("/{buildingId:guid}/gates/{gateId:guid}", async (Guid buildingId, Guid gateId, UpdateGateRequest body, ISender sender, CancellationToken ct) =>
            {
                GateDto result = await sender.Send(
                    new UpdateGateCommand(
                        gateId, body.Name, body.Code, body.Description, body.IsEntryAllowed, body.IsExitAllowed,
                        body.DisplayOrder),
                    ct);
                return Results.Ok(result);
            })
            .RequireBuildingPermission("gate.manage")
            .Produces<GateDto>(StatusCodes.Status200OK);

        buildings.MapPost("/{buildingId:guid}/gates/{gateId:guid}/activate", async (Guid buildingId, Guid gateId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ActivateGateCommand(gateId), ct);
                return Results.NoContent();
            })
            .RequireBuildingPermission("gate.manage")
            .Produces(StatusCodes.Status204NoContent);

        buildings.MapPost("/{buildingId:guid}/gates/{gateId:guid}/deactivate", async (Guid buildingId, Guid gateId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateGateCommand(gateId), ct);
                return Results.NoContent();
            })
            .RequireBuildingPermission("gate.manage")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record UpdateBuildingRequest(string Name, string Code, string? Address);

public sealed record CreateFlatRequest(string FlatNumber, int? FloorNumber, string FlatType);

public sealed record UpdateFlatRequest(string FlatNumber, int? FloorNumber, string FlatType);

public sealed record UpdateFlatAreaRequest(decimal? AreaSqFt);

public sealed record CreateGateRequest(
    string Name, string Code, string? Description, bool IsEntryAllowed, bool IsExitAllowed, int DisplayOrder);

public sealed record UpdateGateRequest(
    string Name, string Code, string? Description, bool IsEntryAllowed, bool IsExitAllowed, int DisplayOrder);
