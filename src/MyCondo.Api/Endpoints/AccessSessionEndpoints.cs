using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInDomesticWorker;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInGuest;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInServiceProvider;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInVehicle;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutDomesticWorker;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutGuest;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutServiceProvider;
using MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutVehicle;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Queries.GetCurrentlyInside;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class AccessSessionEndpoints
{
    public static IEndpointRouteBuilder MapAccessSessionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder accessSessions = app.MapGroup("/api/v1/access-sessions").WithTags("AccessSessions");

        accessSessions.MapPost("/guests/checkin", async (CheckInGuestCommand command, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("visitor.checkin")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/guests/{id:guid}/checkout", async (Guid id, CheckOutGuestRequest body, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(new CheckOutGuestCommand(id, body.ExitGateId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("visitor.checkout")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/vehicles/checkin", async (CheckInVehicleCommand command, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("vehicle.checkin")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/vehicles/{id:guid}/checkout", async (Guid id, CheckOutVehicleRequest body, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(new CheckOutVehicleCommand(id, body.ExitGateId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("vehicle.checkout")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/domestic-workers/checkin", async (CheckInDomesticWorkerCommand command, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.checkin")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/domestic-workers/{id:guid}/checkout", async (Guid id, CheckOutDomesticWorkerRequest body, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(new CheckOutDomesticWorkerCommand(id, body.ExitGateId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.checkout")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/service-providers/checkin", async (CheckInServiceProviderCommand command, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.checkin")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapPost("/service-providers/{id:guid}/checkout", async (Guid id, CheckOutServiceProviderRequest body, ISender sender, CancellationToken ct) =>
            {
                AccessSessionDto result = await sender.Send(new CheckOutServiceProviderCommand(id, body.ExitGateId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.checkout")
            .Produces<AccessSessionDto>(StatusCodes.Status200OK);

        accessSessions.MapGet("/currently-inside", async (string? category, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<AccessSessionDto> result = await sender.Send(
                    new GetCurrentlyInsideQuery(category, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("report.security.view")
            .Produces<PagedResult<AccessSessionDto>>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CheckOutGuestRequest(Guid ExitGateId);

public sealed record CheckOutVehicleRequest(Guid ExitGateId);

public sealed record CheckOutDomesticWorkerRequest(Guid ExitGateId);

public sealed record CheckOutServiceProviderRequest(Guid ExitGateId);
