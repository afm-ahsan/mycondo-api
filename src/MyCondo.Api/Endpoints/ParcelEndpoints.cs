using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.ParcelCustodyEvents.DTOs;
using MyCondo.Application.Features.Security.ParcelCustodyEvents.Queries.GetCustodyHistoryForParcel;
using MyCondo.Application.Features.Security.Parcels.Commands.CloseParcel;
using MyCondo.Application.Features.Security.Parcels.Commands.CollectParcel;
using MyCondo.Application.Features.Security.Parcels.Commands.MarkParcelDamaged;
using MyCondo.Application.Features.Security.Parcels.Commands.NotifyResident;
using MyCondo.Application.Features.Security.Parcels.Commands.ReceiveParcel;
using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Application.Features.Security.Parcels.Queries.GetParcelById;
using MyCondo.Application.Features.Security.Parcels.Queries.GetParcelsForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ParcelEndpoints
{
    public static IEndpointRouteBuilder MapParcelEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder parcels = app.MapGroup("/api/v1/parcels").WithTags("Parcels");

        parcels.MapPost("/", async (ReceiveParcelCommand command, ISender sender, CancellationToken ct) =>
            {
                ParcelDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.receive")
            .Produces<ParcelDto>(StatusCodes.Status200OK);

        parcels.MapGet("/", async (string? status, Guid? recipientFlatId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ParcelDto> result = await sender.Send(
                    new GetParcelsForTenantQuery(status, recipientFlatId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.view")
            .Produces<PagedResult<ParcelDto>>(StatusCodes.Status200OK);

        parcels.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ParcelDto result = await sender.Send(new GetParcelByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.view")
            .Produces<ParcelDto>(StatusCodes.Status200OK);

        parcels.MapGet("/{id:guid}/custody-history", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                List<ParcelCustodyEventDto> result = await sender.Send(new GetCustodyHistoryForParcelQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.view")
            .Produces<List<ParcelCustodyEventDto>>(StatusCodes.Status200OK);

        parcels.MapPost("/{id:guid}/notify", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ParcelDto result = await sender.Send(new NotifyResidentCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.notify")
            .Produces<ParcelDto>(StatusCodes.Status200OK);

        parcels.MapPost("/{id:guid}/collect", async (Guid id, CollectParcelRequest body, ISender sender, CancellationToken ct) =>
            {
                ParcelDto result = await sender.Send(new CollectParcelCommand(id, body.CollectorName, body.Acknowledgement), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.handover")
            .Produces<ParcelDto>(StatusCodes.Status200OK);

        parcels.MapPost("/{id:guid}/damaged", async (Guid id, MarkParcelDamagedRequest body, ISender sender, CancellationToken ct) =>
            {
                ParcelDto result = await sender.Send(new MarkParcelDamagedCommand(id, body.DamageNote), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.update")
            .Produces<ParcelDto>(StatusCodes.Status200OK);

        parcels.MapPost("/{id:guid}/close", async (Guid id, CloseParcelRequest body, ISender sender, CancellationToken ct) =>
            {
                ParcelDto result = await sender.Send(new CloseParcelCommand(id, body.Outcome, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("parcel.return")
            .Produces<ParcelDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CollectParcelRequest(string CollectorName, string? Acknowledgement);

public sealed record MarkParcelDamagedRequest(string DamageNote);

public sealed record CloseParcelRequest(string Outcome, string Reason);
