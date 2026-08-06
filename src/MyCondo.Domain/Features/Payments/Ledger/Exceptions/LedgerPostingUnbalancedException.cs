using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Payments.Ledger.Exceptions;

public sealed class LedgerPostingUnbalancedException(decimal totalDebits, decimal totalCredits)
    : DomainException($"Ledger posting does not balance: total debits {totalDebits} != total credits {totalCredits}.")
{
    public decimal TotalDebits { get; } = totalDebits;
    public decimal TotalCredits { get; } = totalCredits;
}
