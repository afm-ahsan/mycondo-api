namespace MyCondo.Application.Features.Finance.BankReconciliations.DTOs;

public sealed record BankReconciliationDto(
    Guid BankReconciliationId,
    Guid FinancialAccountId,
    DateOnly StatementDate,
    decimal StatementBalance,
    decimal OpeningLedgerBalance,
    string Status,
    DateTimeOffset? ReconciledAtUtc);

public sealed record BankStatementLineDto(
    Guid BankStatementLineId,
    Guid BankReconciliationId,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    string Status,
    Guid? MatchedLedgerEntryId,
    Guid? AdjustmentPostingId,
    string? ResolutionNotes);

public sealed record BankReconciliationDetailDto(
    BankReconciliationDto Reconciliation, List<BankStatementLineDto> Lines);
