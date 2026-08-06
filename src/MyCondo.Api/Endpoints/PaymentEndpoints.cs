using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Payments.Commands.RecordPayment;
using MyCondo.Application.Features.Payments.Commands.ReversePayment;
using MyCondo.Application.Features.Payments.DTOs;

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
