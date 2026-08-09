using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.EndFlatOwnership;
using MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForFlat;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Admin-only relationship management for FlatOwnership (Phase 3, mycondo-docs ADR-021). Every
/// endpoint here requires ownership.manage — deliberately not ownership.view, which is reserved
/// exclusively for the self-service /api/v1/me/flats endpoint (see that endpoint's and
/// ownership.view's own doc comments for why granting it to FlatOwner/Tenant must never also unlock a
/// broad/admin listing).
///
/// Gated by authentication only, not a plain RequirePermission: ownership.manage is held both
/// tenant-wide (OrganizationAdmin) and Building-scoped (CondoAdmin), and a Building-scoped grant
/// arrives as a `bperm` claim that a single tenant-wide-checking RequirePermission filter cannot
/// evaluate — the same reason /api/v1/me/flats and /me/invoices use this pattern. Each handler
/// resolves the Flat first and then checks HasPermissionForBuilding("ownership.manage",
/// flat.BuildingId) against that Flat's actual Building, so OrganizationAdmin's tenant-wide grant
/// still passes everywhere and CondoAdmin's grant only passes for their own Building.
/// </summary>
public static class FlatOwnershipEndpoints
{
    public static IEndpointRouteBuilder MapFlatOwnershipEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder flatOwnerships = app.MapGroup("/api/v1/properties/flat-ownerships").WithTags("Property");

        flatOwnerships.MapPost("/", async (CreateFlatOwnershipCommand command, ISender sender, CancellationToken ct) =>
            {
                CreateFlatOwnershipResult result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<CreateFlatOwnershipResult>(StatusCodes.Status200OK);

        flatOwnerships.MapDelete("/{id:guid}", async (Guid id, DateOnly endDate, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new EndFlatOwnershipCommand(id, endDate), ct);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/api/v1/properties/flats/{flatId:guid}/ownerships", async (Guid flatId, ISender sender, CancellationToken ct) =>
            {
                List<FlatOwnershipDto> result = await sender.Send(new GetFlatOwnershipsForFlatQuery(flatId), ct);
                return Results.Ok(result);
            })
            .WithTags("Property")
            .RequireAuthorization()
            .Produces<List<FlatOwnershipDto>>(StatusCodes.Status200OK);

        return app;
    }
}
