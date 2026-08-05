using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForGuestProfile;
using MyCondo.Application.Features.Security.Guests.Commands.BlockGuestProfile;
using MyCondo.Application.Features.Security.Guests.Commands.CreateGuestProfile;
using MyCondo.Application.Features.Security.Guests.Commands.UnblockGuestProfile;
using MyCondo.Application.Features.Security.Guests.DTOs;
using MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfileByPhone;
using MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfilesForTenant;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class GuestEndpoints
{
    public static IEndpointRouteBuilder MapGuestEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder guests = app.MapGroup("/api/v1/guests").WithTags("Guests");

        guests.MapPost("/", async (CreateGuestProfileCommand command, ISender sender, CancellationToken ct) =>
            {
                GuestProfileDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("visitor.create")
            .Produces<GuestProfileDto>(StatusCodes.Status200OK);

        guests.MapGet("/", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GuestProfileDto> result = await sender.Send(
                    new GetGuestProfilesForTenantQuery(search, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("visitor.view")
            .Produces<PagedResult<GuestProfileDto>>(StatusCodes.Status200OK);

        guests.MapGet("/by-phone/{phone}", async (string phone, ISender sender, CancellationToken ct) =>
            {
                GuestProfileDto? result = await sender.Send(new GetGuestProfileByPhoneQuery(phone), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequirePermission("visitor.view")
            .Produces<GuestProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        guests.MapPost("/{id:guid}/block", async (Guid id, BlockGuestProfileRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new BlockGuestProfileCommand(id, body.Reason), ct);
                return Results.NoContent();
            })
            .RequirePermission("visitor.block.manage")
            .Produces(StatusCodes.Status204NoContent);

        guests.MapPost("/{id:guid}/unblock", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UnblockGuestProfileCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission("visitor.block.manage")
            .Produces(StatusCodes.Status204NoContent);

        guests.MapGet("/{id:guid}/visits", async (Guid id, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<AccessSessionDto> result = await sender.Send(
                    new GetAccessSessionsForGuestProfileQuery(id, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("visitor.view")
            .Produces<PagedResult<AccessSessionDto>>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record BlockGuestProfileRequest(string Reason);
