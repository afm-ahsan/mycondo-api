using Mediator;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Me.Queries.GetMyFlats;
using MyCondo.Application.Features.Me.Queries.GetMyInvoices;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Self-service resident-facing endpoints (Phase 3, mycondo-docs ADR-021) — every response is derived
/// entirely from the caller's own JWT identity (UserId/TenantId), never from a request parameter, so
/// there is no way to ask about anyone else's data. Gated by authentication only, not a specific
/// RequirePermission: each query handler enforces its own per-relationship
/// ownership.view / lease.view / invoice.view.own check internally (see GetMyFlatsQueryHandler/
/// GetMyInvoicesQueryHandler) — the same pattern the existing GET /api/v1/auth/me endpoint already
/// uses for self-scoped data.
/// </summary>
public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder me = app.MapGroup("/api/v1/me").WithTags("Me");

        me.MapGet("/flats", async (ISender sender, CancellationToken ct) =>
            {
                List<MyFlatDto> result = await sender.Send(new GetMyFlatsQuery(), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<List<MyFlatDto>>(StatusCodes.Status200OK);

        me.MapGet("/invoices", async (ISender sender, CancellationToken ct) =>
            {
                List<InvoiceDto> result = await sender.Send(new GetMyInvoicesQuery(), ct);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<List<InvoiceDto>>(StatusCodes.Status200OK);

        return app;
    }
}
