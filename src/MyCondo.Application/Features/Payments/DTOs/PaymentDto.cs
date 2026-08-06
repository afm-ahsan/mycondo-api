namespace MyCondo.Application.Features.Payments.DTOs;

public sealed record PaymentDto(
    Guid PaymentId,
    Guid FlatId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    DateOnly BusinessDate,
    Guid? ReceivedBy,
    string Status,
    Guid LedgerPostingId,
    DateTimeOffset? ReversedAtUtc,
    Guid? ReversedBy,
    string? ReversalReason,
    IReadOnlyList<PaymentAllocationDto> Allocations);
