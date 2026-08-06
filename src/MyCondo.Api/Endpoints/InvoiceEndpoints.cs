using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Billing.Commands.GenerateInvoiceBatch;
using MyCondo.Application.Features.Billing.Commands.VoidInvoice;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Queries.GetInvoiceById;
using MyCondo.Application.Features.Billing.Queries.GetInvoices;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder invoices = app.MapGroup("/api/v1/invoices").WithTags("Invoices");

        invoices.MapPost("/generate-batch", async (GenerateInvoiceBatchCommand command, ISender sender, CancellationToken ct) =>
            {
                GenerateInvoiceBatchResultDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.invoice.generate")
            .RequireIdempotencyKey()
            .Produces<GenerateInvoiceBatchResultDto>(StatusCodes.Status200OK);

        invoices.MapGet("/", async (Guid? buildingId, Guid? flatId, string? status, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<InvoiceDto> result = await sender.Send(
                    new GetInvoicesQuery(buildingId, flatId, status, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.invoice.view")
            .Produces<PagedResult<InvoiceDto>>(StatusCodes.Status200OK);

        invoices.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                InvoiceDetailDto result = await sender.Send(new GetInvoiceByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.invoice.view")
            .Produces<InvoiceDetailDto>(StatusCodes.Status200OK);

        invoices.MapPost("/{id:guid}/void", async (Guid id, VoidInvoiceRequest body, ISender sender, CancellationToken ct) =>
            {
                InvoiceDto result = await sender.Send(new VoidInvoiceCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.invoice.void")
            .RequireIdempotencyKey()
            .Produces<InvoiceDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record VoidInvoiceRequest(string Reason);
