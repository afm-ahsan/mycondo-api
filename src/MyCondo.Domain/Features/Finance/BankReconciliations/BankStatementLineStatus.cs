namespace MyCondo.Domain.Features.Finance.BankReconciliations;

public enum BankStatementLineStatus
{
    Unmatched = 0,
    Matched = 1,
    Excluded = 2,
    Adjusted = 3,
}
