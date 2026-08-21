using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.BankReconciliations.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.UnitTests.Features.Finance.BankReconciliations;

public class BankStatementLineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BankReconciliationId BankReconciliationId = BankReconciliationId.New();
    private static readonly DateOnly TransactionDate = new(2026, 8, 15);

    private static BankStatementLine AddLine(decimal amount = 500m) =>
        BankStatementLine.Add(TenantId, BankReconciliationId, TransactionDate, "Deposit", amount);

    [Fact]
    public void Add_Starts_Unmatched()
    {
        BankStatementLine line = AddLine();

        line.Status.Should().Be(BankStatementLineStatus.Unmatched);
    }

    [Fact]
    public void Add_With_Zero_Amount_Throws()
    {
        Action act = () => BankStatementLine.Add(TenantId, BankReconciliationId, TransactionDate, "Deposit", 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MatchTo_Sets_Matched_And_Records_The_Ledger_Entry()
    {
        BankStatementLine line = AddLine();
        LedgerEntryId ledgerEntryId = LedgerEntryId.New();

        line.MatchTo(ledgerEntryId);

        line.Status.Should().Be(BankStatementLineStatus.Matched);
        line.MatchedLedgerEntryId.Should().Be(ledgerEntryId);
    }

    [Fact]
    public void Exclude_Sets_Excluded_And_Records_The_Reason()
    {
        BankStatementLine line = AddLine();

        line.Exclude("Bank-side duplicate entry");

        line.Status.Should().Be(BankStatementLineStatus.Excluded);
        line.ResolutionNotes.Should().Be("Bank-side duplicate entry");
    }

    [Fact]
    public void MarkAdjusted_Sets_Adjusted_And_Records_The_Posting()
    {
        BankStatementLine line = AddLine();
        LedgerPostingId postingId = LedgerPostingId.New();

        line.MarkAdjusted(postingId);

        line.Status.Should().Be(BankStatementLineStatus.Adjusted);
        line.AdjustmentPostingId.Should().Be(postingId);
    }

    [Theory]
    [MemberData(nameof(ResolutionActions))]
    public void Resolving_An_Already_Resolved_Line_Throws(Action<BankStatementLine> firstResolution, Action<BankStatementLine> secondResolution)
    {
        BankStatementLine line = AddLine();
        firstResolution(line);

        Action act = () => secondResolution(line);

        act.Should().Throw<BankStatementLineNotUnmatchedException>();
    }

    public static IEnumerable<object[]> ResolutionActions()
    {
        Action<BankStatementLine> match = l => l.MatchTo(LedgerEntryId.New());
        Action<BankStatementLine> exclude = l => l.Exclude("reason");
        Action<BankStatementLine> adjust = l => l.MarkAdjusted(LedgerPostingId.New());

        yield return [match, match];
        yield return [match, exclude];
        yield return [exclude, adjust];
        yield return [adjust, match];
    }
}
