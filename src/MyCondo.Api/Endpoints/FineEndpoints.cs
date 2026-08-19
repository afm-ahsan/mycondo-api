using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.Commands.AssessFine;
using MyCondo.Application.Features.Billing.Commands.ReverseFine;
using MyCondo.Application.Features.Billing.Commands.WaiveFine;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Queries.GetInvoiceById;
using MyCondo.Application.Features.Billing.Queries.GetInvoices;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Fines are <c>Invoice</c> rows with <c>Source == "Fine"</c> — see <c>InvoiceSource.Fine</c>'s doc
/// comment. This group exposes them under their own route/permission surface
/// (<c>billing.fine.*</c>, distinct from <c>billing.invoice.*</c>) rather than folding fine
/// assess/waive/reverse into <c>InvoiceEndpoints</c>, so a role granted fine-handling authority isn't
/// implicitly granted generic invoice authority (or vice versa). List/detail delegate to the existing
/// <c>GetInvoicesQuery</c>/<c>GetInvoiceByIdQuery</c> handlers (list forces <c>source=Fine</c> rather
/// than trusting a caller-supplied value; detail 404s if the id isn't actually a Fine) instead of
/// duplicating query logic.
/// </summary>
public static class FineEndpoints
{
    public static IEndpointRouteBuilder MapFineEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder fines = app.MapGroup("/api/v1/fines").WithTags("Fines");

        fines.MapPost("/", async (AssessFineCommand command, ISender sender, CancellationToken ct) =>
            {
                InvoiceDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.fine.assess")
            .RequireIdempotencyKey()
            .Produces<InvoiceDto>(StatusCodes.Status200OK);

        fines.MapGet("/", async (Guid? buildingId, Guid? flatId, string? status, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<InvoiceDto> result = await sender.Send(
                    new GetInvoicesQuery(buildingId, flatId, status, "Fine", page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.fine.view")
            .Produces<PagedResult<InvoiceDto>>(StatusCodes.Status200OK);

        fines.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                InvoiceDetailDto result = await sender.Send(new GetInvoiceByIdQuery(id), ct);
                if (result.Invoice.Source != "Fine")
                {
                    throw new NotFoundException("Fine", id);
                }

                return Results.Ok(result);
            })
            .RequirePermission("billing.fine.view")
            .Produces<InvoiceDetailDto>(StatusCodes.Status200OK);

        fines.MapPost("/{id:guid}/waive", async (Guid id, WaiveFineRequest body, ISender sender, CancellationToken ct) =>
            {
                InvoiceDto result = await sender.Send(new WaiveFineCommand(id, body.Amount, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.fine.waive")
            .RequireIdempotencyKey()
            .Produces<InvoiceDto>(StatusCodes.Status200OK);

        fines.MapPost("/{id:guid}/reverse", async (Guid id, ReverseFineRequest body, ISender sender, CancellationToken ct) =>
            {
                InvoiceDto result = await sender.Send(new ReverseFineCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.fine.reverse")
            .RequireIdempotencyKey()
            .Produces<InvoiceDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record WaiveFineRequest(decimal Amount, string Reason);

public sealed record ReverseFineRequest(string Reason);
