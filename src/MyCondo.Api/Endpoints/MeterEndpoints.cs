using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Utilities.Commands.AssignMeter;
using MyCondo.Application.Features.Utilities.Commands.InstallMeter;
using MyCondo.Application.Features.Utilities.Commands.MarkMeterFaulty;
using MyCondo.Application.Features.Utilities.Commands.ReactivateMeter;
using MyCondo.Application.Features.Utilities.Commands.ReplaceMeter;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionHistory;
using MyCondo.Application.Features.Utilities.Queries.GetMeterAssignmentHistory;
using MyCondo.Application.Features.Utilities.Queries.GetMeters;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class MeterEndpoints
{
    public static IEndpointRouteBuilder MapMeterEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder meters = app.MapGroup("/api/v1/meters").WithTags("Meters");

        meters.MapPost("/", async (InstallMeterCommand command, ISender sender, CancellationToken ct) =>
            {
                MeterDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.manage")
            .Produces<MeterDto>(StatusCodes.Status200OK);

        meters.MapGet("/", async (Guid buildingId, string? utilityType, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<MeterDto> result = await sender.Send(
                    new GetMetersQuery(buildingId, utilityType, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.view")
            .Produces<PagedResult<MeterDto>>(StatusCodes.Status200OK);

        meters.MapPost("/{id:guid}/mark-faulty", async (Guid id, MarkMeterFaultyRequest body, ISender sender, CancellationToken ct) =>
            {
                MeterDto result = await sender.Send(new MarkMeterFaultyCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.manage")
            .Produces<MeterDto>(StatusCodes.Status200OK);

        meters.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                MeterDto result = await sender.Send(new ReactivateMeterCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.manage")
            .Produces<MeterDto>(StatusCodes.Status200OK);

        meters.MapPost("/{id:guid}/replace", async (Guid id, ReplaceMeterRequest body, ISender sender, CancellationToken ct) =>
            {
                ReplaceMeterResultDto result = await sender.Send(new ReplaceMeterCommand(id, body.NewMeterNumber), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.manage")
            .Produces<ReplaceMeterResultDto>(StatusCodes.Status200OK);

        meters.MapPost("/{id:guid}/assign", async (Guid id, AssignMeterRequest body, ISender sender, CancellationToken ct) =>
            {
                MeterAssignmentDto result = await sender.Send(new AssignMeterCommand(id, body.FlatId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.manage")
            .Produces<MeterAssignmentDto>(StatusCodes.Status200OK);

        meters.MapGet("/{id:guid}/assignment-history", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<MeterAssignmentDto> result = await sender.Send(new GetMeterAssignmentHistoryQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.meter.view")
            .Produces<IReadOnlyList<MeterAssignmentDto>>(StatusCodes.Status200OK);

        meters.MapGet("/{id:guid}/consumption-history", async (Guid id, DateOnly fromDate, DateOnly toDate, ISender sender, CancellationToken ct) =>
            {
                IReadOnlyList<ReadingDto> result = await sender.Send(new GetConsumptionHistoryQuery(id, fromDate, toDate), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.view")
            .Produces<IReadOnlyList<ReadingDto>>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record MarkMeterFaultyRequest(string Reason);

public sealed record ReplaceMeterRequest(string NewMeterNumber);

public sealed record AssignMeterRequest(Guid FlatId);
