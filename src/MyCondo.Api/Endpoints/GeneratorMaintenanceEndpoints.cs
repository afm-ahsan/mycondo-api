using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.CompleteMaintenanceService;
using MyCondo.Application.Features.Operations.Commands.CreateMaintenanceSchedule;
using MyCondo.Application.Features.Operations.Commands.RecordBreakdown;
using MyCondo.Application.Features.Operations.Commands.ResolveBreakdown;
using MyCondo.Application.Features.Operations.Commands.UpdateMaintenanceSchedule;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorBreakdownRecords;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceSchedules;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorServiceRecords;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class GeneratorMaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapGeneratorMaintenanceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder schedules = app.MapGroup("/api/v1/generator-maintenance-schedules").WithTags("Generator Maintenance");

        schedules.MapPost("/", async (CreateMaintenanceScheduleCommand command, ISender sender, CancellationToken ct) =>
            {
                GeneratorMaintenanceScheduleDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.maintenance.manage")
            .Produces<GeneratorMaintenanceScheduleDto>(StatusCodes.Status200OK);

        schedules.MapGet("/", async (Guid? generatorId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GeneratorMaintenanceScheduleDto> result = await sender.Send(
                    new GetGeneratorMaintenanceSchedulesQuery(generatorId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<PagedResult<GeneratorMaintenanceScheduleDto>>(StatusCodes.Status200OK);

        schedules.MapPut("/{id:guid}", async (Guid id, UpdateMaintenanceScheduleRequest body, ISender sender, CancellationToken ct) =>
            {
                GeneratorMaintenanceScheduleDto result = await sender.Send(
                    new UpdateMaintenanceScheduleCommand(id, body.NextDueDate, body.NextDueHourMeterReading), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.maintenance.manage")
            .Produces<GeneratorMaintenanceScheduleDto>(StatusCodes.Status200OK);

        schedules.MapPost("/{id:guid}/complete", async (Guid id, CompleteMaintenanceServiceRequest body, ISender sender, CancellationToken ct) =>
            {
                GeneratorServiceRecordDto result = await sender.Send(
                    new CompleteMaintenanceServiceCommand(
                        id, body.PerformedAtUtc, body.Description, body.Cost, body.NextDueDate, body.NextDueHourMeterReading),
                    ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.maintenance.manage")
            .Produces<GeneratorServiceRecordDto>(StatusCodes.Status200OK);

        RouteGroupBuilder serviceRecords = app.MapGroup("/api/v1/generator-service-records").WithTags("Generator Maintenance");

        serviceRecords.MapGet("/", async (Guid? generatorId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GeneratorServiceRecordDto> result = await sender.Send(
                    new GetGeneratorServiceRecordsQuery(generatorId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<PagedResult<GeneratorServiceRecordDto>>(StatusCodes.Status200OK);

        RouteGroupBuilder breakdowns = app.MapGroup("/api/v1/generator-breakdowns").WithTags("Generator Maintenance");

        breakdowns.MapPost("/", async (RecordBreakdownCommand command, ISender sender, CancellationToken ct) =>
            {
                GeneratorBreakdownRecordDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.maintenance.manage")
            .Produces<GeneratorBreakdownRecordDto>(StatusCodes.Status200OK);

        breakdowns.MapPost("/{id:guid}/resolve", async (Guid id, ResolveBreakdownRequest body, ISender sender, CancellationToken ct) =>
            {
                GeneratorBreakdownRecordDto result = await sender.Send(
                    new ResolveBreakdownCommand(id, body.Resolution, body.Cost, body.DowntimeEndUtc), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.maintenance.manage")
            .Produces<GeneratorBreakdownRecordDto>(StatusCodes.Status200OK);

        breakdowns.MapGet("/", async (Guid? generatorId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GeneratorBreakdownRecordDto> result = await sender.Send(
                    new GetGeneratorBreakdownRecordsQuery(generatorId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<PagedResult<GeneratorBreakdownRecordDto>>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record UpdateMaintenanceScheduleRequest(DateOnly? NextDueDate, decimal? NextDueHourMeterReading);

public sealed record CompleteMaintenanceServiceRequest(
    DateTimeOffset PerformedAtUtc, string Description, decimal? Cost, DateOnly? NextDueDate, decimal? NextDueHourMeterReading);

public sealed record ResolveBreakdownRequest(string Resolution, decimal? Cost, DateTimeOffset DowntimeEndUtc);
