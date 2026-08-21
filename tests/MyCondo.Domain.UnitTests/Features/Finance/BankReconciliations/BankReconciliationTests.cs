using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Domain.UnitTests.Features.Finance.BankReconciliations;

public class BankReconciliationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FinancialAccountId FinancialAccountId = FinancialAccountId.New();
    private static readonly DateOnly StatementDate = new(2026, 8, 31);
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_Sets_InProgress_And_Carries_The_Entered_Statement_Balance()
    {
        BankReconciliation reconciliation = BankReconciliation.Start(TenantId, FinancialAccountId, StatementDate, 10_000m, 9_500m);

        reconciliation.Status.Should().Be(BankReconciliationStatus.InProgress);
        reconciliation.StatementBalance.Should().Be(10_000m);
        reconciliation.OpeningLedgerBalance.Should().Be(9_500m);
        reconciliation.ReconciledAtUtc.Should().BeNull();
    }

    [Fact]
    public void Complete_With_Matching_Balance_Sets_Reconciled()
    {
        BankReconciliation reconciliation = BankReconciliation.Start(TenantId, FinancialAccountId, StatementDate, 10_000m, 9_500m);
        Guid reconciler = Guid.NewGuid();

        reconciliation.Complete(10_000m, reconciler, NowUtc);

        reconciliation.Status.Should().Be(BankReconciliationStatus.Reconciled);
        reconciliation.ReconciledAtUtc.Should().Be(NowUtc);
        reconciliation.ReconciledBy.Should().Be(reconciler);
    }

    [Fact]
    public void Complete_With_A_Different_Computed_Balance_Throws_And_Does_Not_Reconcile()
    {
        BankReconciliation reconciliation = BankReconciliation.Start(TenantId, FinancialAccountId, StatementDate, 10_000m, 9_500m);

        Action act = () => reconciliation.Complete(9_999m, Guid.NewGuid(), NowUtc);

        act.Should().Throw<BankReconciliationBalanceMismatchException>();
        reconciliation.Status.Should().Be(BankReconciliationStatus.InProgress);
    }

    [Fact]
    public void Complete_Twice_Throws()
    {
        BankReconciliation reconciliation = BankReconciliation.Start(TenantId, FinancialAccountId, StatementDate, 10_000m, 9_500m);
        reconciliation.Complete(10_000m, Guid.NewGuid(), NowUtc);

        Action act = () => reconciliation.Complete(10_000m, Guid.NewGuid(), NowUtc);

        act.Should().Throw<BankReconciliationAlreadyReconciledException>();
    }
}
