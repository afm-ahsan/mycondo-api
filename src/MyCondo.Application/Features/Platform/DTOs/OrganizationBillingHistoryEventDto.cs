namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>
/// One event in a single organization's Platform Control Plane billing timeline (ADR-034 Task 14L) —
/// either an invoice being issued or a payment being recorded, merged and ordered by each event's own
/// authoritative date (invoice <c>IssueDate</c>/payment <c>PaymentDate</c>) rather than a single
/// generic timestamp field. <see cref="EventType"/> is <c>"InvoiceIssued"</c> or
/// <c>"PaymentRecorded"</c>; the fields that apply only to one event type are null for the other
/// (mirrors how <see cref="PlatformSubscriptionInvoiceListItemDto.DaysOverdue"/> is null when not
/// applicable rather than modeling two separate DTOs for a small, closely-related pair of read rows).
/// </summary>
public sealed record OrganizationBillingHistoryEventDto(
    DateOnly EventDate,
    string EventType,
    Guid InvoiceId,
    string InvoiceNumber,
    string Currency,
    decimal Amount,
    string? InvoiceStatus,
    decimal? OutstandingAmount,
    int? DaysOverdue,
    Guid? PaymentId,
    string? ReferenceNumber);
