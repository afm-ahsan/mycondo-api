using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.UnitTests.Features.Finance.FixedDeposits;

public class FixedDepositInterestAccrualTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FixedDepositId FixedDepositId = FixedDepositId.New();

    private static FixedDepositInterestAccrual Record(decimal grossAmount = 3125m) =>
        FixedDepositInterestAccrual.Record(
            FixedDepositInterestAccrualId.New(), TenantId, FixedDepositId, new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 31), grossAmount, " January interest ",
            LedgerPostingId.New(), Now);

    [Fact]
    public void Record_Trims_Notes_And_Is_Not_Reversed()
    {
        FixedDepositInterestAccrual accrual = Record();

        accrual.Notes.Should().Be("January interest");
        accrual.IsReversed.Should().BeFalse();
        accrual.ReversalPostingId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_Rejects_Non_Positive_GrossAmount(decimal amount)
    {
        Action act = () => Record(amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_Rejects_PeriodEnd_Before_PeriodStart()
    {
        Action act = () => FixedDepositInterestAccrual.Record(
            FixedDepositInterestAccrualId.New(), TenantId, FixedDepositId, new DateOnly(2026, 1, 31),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 100m, null, LedgerPostingId.New(), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkReversed_Sets_Flag_And_ReversalPostingId()
    {
        FixedDepositInterestAccrual accrual = Record();
        LedgerPostingId reversalId = LedgerPostingId.New();

        accrual.MarkReversed(reversalId, Now);

        accrual.IsReversed.Should().BeTrue();
        accrual.ReversalPostingId.Should().Be(reversalId);
    }

    [Fact]
    public void MarkReversed_Throws_When_Already_Reversed()
    {
        FixedDepositInterestAccrual accrual = Record();
        accrual.MarkReversed(LedgerPostingId.New(), Now);

        Action act = () => accrual.MarkReversed(LedgerPostingId.New(), Now);

        act.Should().Throw<InvalidOperationException>();
    }
}
