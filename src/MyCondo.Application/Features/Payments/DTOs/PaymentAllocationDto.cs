namespace MyCondo.Application.Features.Payments.DTOs;

public sealed record PaymentAllocationDto(
    Guid PaymentAllocationId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal AllocatedAmount,
    DateTimeOffset AllocatedAtUtc);
