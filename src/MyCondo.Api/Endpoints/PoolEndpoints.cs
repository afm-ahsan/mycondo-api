using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Amenities.Commands.CheckInPoolSession;
using MyCondo.Application.Features.Amenities.Commands.CheckOutPoolSession;
using MyCondo.Application.Features.Amenities.Commands.ReportPoolIncident;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Queries.GetPoolIncidents;
using MyCondo.Application.Features.Amenities.Queries.GetPoolSessionById;
using MyCondo.Application.Features.Amenities.Queries.GetPoolSessions;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class PoolEndpoints
{
    public static IEndpointRouteBuilder MapPoolEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder sessions = app.MapGroup("/api/v1/swimming-pool/sessions").WithTags("Swimming Pool");

        sessions.MapPost("/", async (CheckInPoolSessionCommand command, ISender sender, CancellationToken ct) =>
            {
                PoolSessionDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("pool.checkin")
            .Produces<PoolSessionDto>(StatusCodes.Status200OK);

        sessions.MapPost("/{id:guid}/check-out", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                PoolSessionDto result = await sender.Send(new CheckOutPoolSessionCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("pool.checkout")
            .Produces<PoolSessionDto>(StatusCodes.Status200OK);

        sessions.MapGet("/", async (Guid? facilityId, Guid? flatId, bool? openOnly, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<PoolSessionDto> result = await sender.Send(
                    new GetPoolSessionsQuery(facilityId, flatId, openOnly, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("pool.view")
            .Produces<PagedResult<PoolSessionDto>>(StatusCodes.Status200OK);

        sessions.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                PoolSessionDto result = await sender.Send(new GetPoolSessionByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("pool.view")
            .Produces<PoolSessionDto>(StatusCodes.Status200OK);

        RouteGroupBuilder incidents = app.MapGroup("/api/v1/swimming-pool/incidents").WithTags("Swimming Pool");

        incidents.MapPost("/", async (ReportPoolIncidentCommand command, ISender sender, CancellationToken ct) =>
            {
                PoolIncidentDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("pool.incident.manage")
            .Produces<PoolIncidentDto>(StatusCodes.Status200OK);

        incidents.MapGet("/", async (Guid? facilityId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<PoolIncidentDto> result = await sender.Send(
                    new GetPoolIncidentsQuery(facilityId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("pool.view")
            .Produces<PagedResult<PoolIncidentDto>>(StatusCodes.Status200OK);

        return app;
    }
}
