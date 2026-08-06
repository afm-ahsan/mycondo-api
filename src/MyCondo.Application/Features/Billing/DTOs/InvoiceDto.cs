namespace MyCondo.Application.Features.Billing.DTOs;

public sealed record InvoiceDto(
    Guid InvoiceId,
    Guid BuildingId,
    Guid FlatId,
    string InvoiceNumber,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal SubtotalAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    string Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? VoidedAtUtc,
    Guid? VoidedBy,
    string? VoidReason);
