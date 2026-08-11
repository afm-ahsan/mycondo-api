using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Residents.Commands.CreateResident;
using MyCondo.Application.Features.Residents.Commands.DisableResident;
using MyCondo.Application.Features.Residents.Commands.LinkResidentToUser;
using MyCondo.Application.Features.Residents.Commands.UpdateResident;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Residents.Queries.GetResidentById;
using MyCondo.Application.Features.Residents.Queries.GetResidentsForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ResidentEndpoints
{
    public static IEndpointRouteBuilder MapResidentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder residents = app.MapGroup("/api/v1/residents").WithTags("Residents");

        residents.MapPost("/", async (CreateResidentCommand command, ISender sender, CancellationToken ct) =>
            {
                ResidentDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("resident.create")
            .Produces<ResidentDto>(StatusCodes.Status200OK);

        residents.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ResidentDto> result = await sender.Send(
                    new GetResidentsForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("resident.view")
            .Produces<PagedResult<ResidentDto>>(StatusCodes.Status200OK);

        residents.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ResidentDto result = await sender.Send(new GetResidentByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("resident.view")
            .Produces<ResidentDto>(StatusCodes.Status200OK);

        residents.MapPost("/{id:guid}/link-user", async (Guid id, LinkResidentToUserRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new LinkResidentToUserCommand(id, body.UserId), ct);
                return Results.NoContent();
            })
            .RequirePermission("resident.update")
            .Produces(StatusCodes.Status204NoContent);

        residents.MapPut("/{id:guid}", async (Guid id, UpdateResidentRequest body, ISender sender, CancellationToken ct) =>
            {
                ResidentDto result = await sender.Send(
                    new UpdateResidentCommand(id, body.FullName, body.Phone, body.Email), ct);
                return Results.Ok(result);
            })
            .RequirePermission("resident.update")
            .Produces<ResidentDto>(StatusCodes.Status200OK);

        residents.MapPost("/{id:guid}/disable", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DisableResidentCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("resident.disable")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record LinkResidentToUserRequest(Guid UserId);

public sealed record UpdateResidentRequest(string FullName, string? Phone, string? Email);
