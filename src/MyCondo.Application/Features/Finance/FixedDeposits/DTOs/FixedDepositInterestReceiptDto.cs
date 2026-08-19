namespace MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

public sealed record FixedDepositInterestReceiptDto(
    Guid FixedDepositInterestReceiptId,
    Guid FixedDepositId,
    DateOnly AccountingDate,
    decimal GrossAmount,
    decimal DeductionAmount,
    decimal NetAmount,
    Guid ReceivingFinancialAccountId,
    string? ReferenceNumber,
    string? Notes,
    bool IsReversed,
    DateTimeOffset CreatedAtUtc);
