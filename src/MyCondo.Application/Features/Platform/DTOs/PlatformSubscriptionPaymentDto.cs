namespace MyCondo.Application.Features.Platform.DTOs;

public sealed record PlatformSubscriptionPaymentDto(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateOnly PaymentDate,
    string ReferenceNumber,
    string? Notes,
    DateTimeOffset RecordedAtUtc);
