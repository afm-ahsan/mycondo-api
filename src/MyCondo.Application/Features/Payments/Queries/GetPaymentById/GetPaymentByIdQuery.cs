using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto>;
