using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.ApproveServiceProviderAssignment;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.CreateServiceProviderAssignment;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.DeactivateServiceProviderAssignment;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.Queries.GetAssignmentsForServiceProvider;
using MyCondo.Application.Features.Security.ServiceProviders.Commands.RegisterServiceProvider;
using MyCondo.Application.Features.Security.ServiceProviders.Commands.SetServiceProviderStatus;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Application.Features.Security.ServiceProviders.Queries.GetServiceProviderProfilesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ServiceProviderEndpoints
{
    public static IEndpointRouteBuilder MapServiceProviderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder providers = app.MapGroup("/api/v1/service-providers").WithTags("ServiceProviders");

        providers.MapPost("/", async (RegisterServiceProviderCommand command, ISender sender, CancellationToken ct) =>
            {
                ServiceProviderProfileDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.manage")
            .Produces<ServiceProviderProfileDto>(StatusCodes.Status200OK);

        providers.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ServiceProviderProfileDto> result = await sender.Send(
                    new GetServiceProviderProfilesForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.view")
            .Produces<PagedResult<ServiceProviderProfileDto>>(StatusCodes.Status200OK);

        providers.MapPost("/{id:guid}/status", async (Guid id, SetServiceProviderStatusRequest body, ISender sender, CancellationToken ct) =>
            {
                ServiceProviderProfileDto result = await sender.Send(
                    new SetServiceProviderStatusCommand(id, body.Status, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.manage")
            .Produces<ServiceProviderProfileDto>(StatusCodes.Status200OK);

        providers.MapPost("/{id:guid}/assignments", async (Guid id, CreateServiceProviderAssignmentRequest body, ISender sender, CancellationToken ct) =>
            {
                ServiceProviderAssignmentDto result = await sender.Send(
                    new CreateServiceProviderAssignmentCommand(
                        id, body.FlatId, body.ValidFromUtc, body.ValidToUtc, body.AllowedDays,
                        body.AllowedStartTime, body.AllowedEndTime), ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.assignment.manage")
            .Produces<ServiceProviderAssignmentDto>(StatusCodes.Status200OK);

        providers.MapGet("/{id:guid}/assignments", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                List<ServiceProviderAssignmentDto> result = await sender.Send(new GetAssignmentsForServiceProviderQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("serviceprovider.view")
            .Produces<List<ServiceProviderAssignmentDto>>(StatusCodes.Status200OK);

        providers.MapPost("/assignments/{assignmentId:guid}/approve", async (Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ApproveServiceProviderAssignmentCommand(assignmentId), ct);
                return Results.NoContent();
            })
            .RequirePermission("serviceprovider.assignment.manage")
            .Produces(StatusCodes.Status204NoContent);

        providers.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateServiceProviderAssignmentCommand(assignmentId), ct);
                return Results.NoContent();
            })
            .RequirePermission("serviceprovider.assignment.manage")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record SetServiceProviderStatusRequest(string Status, string? Reason);

public sealed record CreateServiceProviderAssignmentRequest(
    Guid FlatId,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string? AllowedDays,
    TimeOnly? AllowedStartTime,
    TimeOnly? AllowedEndTime);
