using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Payments.Payments.Exceptions;

public sealed class PaymentAlreadyReversedException(PaymentId paymentId)
    : DomainException($"Payment {paymentId} is already reversed.")
{
    public PaymentId PaymentId { get; } = paymentId;
}
