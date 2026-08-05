using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.ApproveDomesticWorkerAssignment;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.CreateDomesticWorkerAssignment;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.DeactivateDomesticWorkerAssignment;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Queries.GetAssignmentsForDomesticWorker;
using MyCondo.Application.Features.Security.DomesticWorkers.Commands.RegisterDomesticWorker;
using MyCondo.Application.Features.Security.DomesticWorkers.Commands.SetDomesticWorkerStatus;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkers.Queries.GetDomesticWorkerProfilesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class DomesticWorkerEndpoints
{
    public static IEndpointRouteBuilder MapDomesticWorkerEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder workers = app.MapGroup("/api/v1/domestic-workers").WithTags("DomesticWorkers");

        workers.MapPost("/", async (RegisterDomesticWorkerCommand command, ISender sender, CancellationToken ct) =>
            {
                DomesticWorkerProfileDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.manage")
            .Produces<DomesticWorkerProfileDto>(StatusCodes.Status200OK);

        workers.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<DomesticWorkerProfileDto> result = await sender.Send(
                    new GetDomesticWorkerProfilesForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.view")
            .Produces<PagedResult<DomesticWorkerProfileDto>>(StatusCodes.Status200OK);

        workers.MapPost("/{id:guid}/status", async (Guid id, SetDomesticWorkerStatusRequest body, ISender sender, CancellationToken ct) =>
            {
                DomesticWorkerProfileDto result = await sender.Send(
                    new SetDomesticWorkerStatusCommand(id, body.Status, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.manage")
            .Produces<DomesticWorkerProfileDto>(StatusCodes.Status200OK);

        workers.MapPost("/{id:guid}/assignments", async (Guid id, CreateDomesticWorkerAssignmentRequest body, ISender sender, CancellationToken ct) =>
            {
                DomesticWorkerAssignmentDto result = await sender.Send(
                    new CreateDomesticWorkerAssignmentCommand(
                        id, body.FlatId, body.ValidFromUtc, body.ValidToUtc, body.AllowedDays,
                        body.AllowedStartTime, body.AllowedEndTime), ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.assignment.manage")
            .Produces<DomesticWorkerAssignmentDto>(StatusCodes.Status200OK);

        workers.MapGet("/{id:guid}/assignments", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                List<DomesticWorkerAssignmentDto> result = await sender.Send(new GetAssignmentsForDomesticWorkerQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("domesticworker.view")
            .Produces<List<DomesticWorkerAssignmentDto>>(StatusCodes.Status200OK);

        workers.MapPost("/assignments/{assignmentId:guid}/approve", async (Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ApproveDomesticWorkerAssignmentCommand(assignmentId), ct);
                return Results.NoContent();
            })
            .RequirePermission("domesticworker.assignment.manage")
            .Produces(StatusCodes.Status204NoContent);

        workers.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateDomesticWorkerAssignmentCommand(assignmentId), ct);
                return Results.NoContent();
            })
            .RequirePermission("domesticworker.assignment.manage")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record SetDomesticWorkerStatusRequest(string Status, string? Reason);

public sealed record CreateDomesticWorkerAssignmentRequest(
    Guid FlatId,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime);
