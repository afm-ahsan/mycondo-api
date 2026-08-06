namespace MyCondo.Application.Features.Payments.DTOs;

public sealed record PaymentAllocationDto(
    Guid PaymentAllocationId,
    Guid InvoiceId,
    decimal AllocatedAmount,
    DateTimeOffset AllocatedAtUtc);
