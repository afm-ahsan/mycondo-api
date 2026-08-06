using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Commands.RecordPayment;

public sealed record RecordPaymentCommand(
    Guid FlatId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    DateOnly BusinessDate,
    string? Description
) : IRequest<PaymentDto>;
