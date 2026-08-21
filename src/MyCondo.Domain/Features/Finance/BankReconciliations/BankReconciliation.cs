using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Domain.Features.Finance.BankReconciliations;

/// <summary>
/// One monthly reconciliation of a <see cref="FinancialAccount"/> against its bank statement (Template
/// 6). Deliberately does not model "outstanding deposits/cheques" as a carry-forward concept between
/// periods — a genuine timing difference simply blocks <see cref="Complete"/> until every
/// <see cref="BankStatementLine"/> is Matched/Excluded/Adjusted and the resulting ledger balance ties out
/// to <see cref="StatementBalance"/> exactly. This keeps the control a verification surface over the
/// existing ledger rather than a second accounting engine — see the Template 6 execution notes for the
/// explicit scope call.
/// </summary>
public sealed class BankReconciliation : AggregateRoot<BankReconciliationId>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public FinancialAccountId FinancialAccountId { get; private set; }
    public DateOnly StatementDate { get; private set; }
    public decimal StatementBalance { get; private set; }
    public decimal OpeningLedgerBalance { get; private set; }
    public BankReconciliationStatus Status { get; private set; }
    public DateTimeOffset? ReconciledAtUtc { get; private set; }
    public Guid? ReconciledBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private BankReconciliation()
    {
    }

    private BankReconciliation(
        BankReconciliationId id, Guid tenantId, FinancialAccountId financialAccountId, DateOnly statementDate,
        decimal statementBalance, decimal openingLedgerBalance) : base(id)
    {
        TenantId = tenantId;
        FinancialAccountId = financialAccountId;
        StatementDate = statementDate;
        StatementBalance = statementBalance;
        OpeningLedgerBalance = openingLedgerBalance;
        Status = BankReconciliationStatus.InProgress;
    }

    public static BankReconciliation Start(
        Guid tenantId, FinancialAccountId financialAccountId, DateOnly statementDate, decimal statementBalance,
        decimal openingLedgerBalance)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new BankReconciliation(
            BankReconciliationId.New(), tenantId, financialAccountId, statementDate, statementBalance, openingLedgerBalance);
    }

    /// <summary>Requires the caller (Application layer — it alone can query <c>LedgerEntry</c>) to have
    /// already confirmed every line for this reconciliation is Matched/Excluded/Adjusted and to supply
    /// the resulting ledger balance as of <see cref="StatementDate"/>; this method only enforces the
    /// balance actually ties out and the status transition itself.</summary>
    public void Complete(decimal computedLedgerBalance, Guid? reconciledBy, DateTimeOffset nowUtc)
    {
        if (Status == BankReconciliationStatus.Reconciled)
        {
            throw new BankReconciliationAlreadyReconciledException(Id);
        }

        if (computedLedgerBalance != StatementBalance)
        {
            throw new BankReconciliationBalanceMismatchException(Id, StatementBalance, computedLedgerBalance);
        }

        Status = BankReconciliationStatus.Reconciled;
        ReconciledAtUtc = nowUtc;
        ReconciledBy = reconciledBy;
    }
}
