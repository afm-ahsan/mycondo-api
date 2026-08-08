using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Payments.Commands.RecordPayment;
using MyCondo.Application.Features.Payments.Commands.ReversePayment;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.GetPaymentById;
using MyCondo.Application.Features.Payments.Queries.GetPayments;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder payments = app.MapGroup("/api/v1/payments").WithTags("Payments");

        payments.MapPost("/", async (RecordPaymentCommand command, ISender sender, CancellationToken ct) =>
            {
                PaymentDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("payment.record")
            .RequireIdempotencyKey()
            .Produces<PaymentDto>(StatusCodes.Status200OK);

        payments.MapGet("/", async (
                Guid? flatId, string? status, string? paymentMethod, DateOnly? fromDate, DateOnly? toDate,
                int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<PaymentDto> result = await sender.Send(
                    new GetPaymentsQuery(flatId, status, paymentMethod, fromDate, toDate, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("payment.view")
            .Produces<PagedResult<PaymentDto>>(StatusCodes.Status200OK);

        payments.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                PaymentDto result = await sender.Send(new GetPaymentByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("payment.view")
            .Produces<PaymentDto>(StatusCodes.Status200OK);

        payments.MapPost("/{id:guid}/reverse", async (Guid id, ReversePaymentRequest body, ISender sender, CancellationToken ct) =>
            {
                PaymentDto result = await sender.Send(new ReversePaymentCommand(id, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("payment.reverse")
            .RequireIdempotencyKey()
            .Produces<PaymentDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record ReversePaymentRequest(string Reason);
