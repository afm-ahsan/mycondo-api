namespace MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

public sealed record FixedDepositInterestAccrualDto(
    Guid FixedDepositInterestAccrualId,
    Guid FixedDepositId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly AccountingDate,
    decimal GrossAmount,
    string? Notes,
    bool IsReversed,
    DateTimeOffset CreatedAtUtc);
