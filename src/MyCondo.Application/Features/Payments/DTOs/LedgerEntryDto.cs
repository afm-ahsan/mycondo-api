namespace MyCondo.Application.Features.Payments.DTOs;

public sealed record LedgerEntryDto(
    Guid LedgerEntryId,
    Guid PostingId,
    string AccountType,
    Guid? FlatId,
    string Direction,
    decimal Amount,
    DateOnly BusinessDate,
    string Description,
    DateTimeOffset CreatedAtUtc,
    string? ReferenceType,
    Guid? ReferenceId);
