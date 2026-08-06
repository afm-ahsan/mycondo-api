using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Commands.ReversePayment;

public sealed record ReversePaymentCommand(Guid PaymentId, string Reason) : IRequest<PaymentDto>;
