using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.UnitTests.Features.Finance.FixedDeposits;

public class FixedDepositInterestReceiptTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FixedDepositId FixedDepositId = FixedDepositId.New();
    private static readonly FinancialAccountId ReceivingAccountId = FinancialAccountId.New();

    private static FixedDepositInterestReceipt Record(decimal gross = 60_000m, decimal deduction = 6_000m) =>
        FixedDepositInterestReceipt.Record(
            FixedDepositInterestReceiptId.New(), TenantId, FixedDepositId, new DateOnly(2026, 1, 31), gross,
            deduction, ReceivingAccountId, " CHQ-0099 ", " monthly cheque ", LedgerPostingId.New(), Now);

    [Fact]
    public void Record_Computes_NetAmount_As_Gross_Minus_Deduction()
    {
        FixedDepositInterestReceipt receipt = Record(60_000m, 6_000m);

        receipt.NetAmount.Should().Be(54_000m);
        receipt.ReferenceNumber.Should().Be("CHQ-0099");
        receipt.Notes.Should().Be("monthly cheque");
    }

    [Fact]
    public void Record_Allows_Zero_Deduction()
    {
        FixedDepositInterestReceipt receipt = Record(60_000m, 0m);

        receipt.NetAmount.Should().Be(60_000m);
    }

    [Fact]
    public void Record_Allows_Full_Deduction_Resulting_In_Zero_Net()
    {
        FixedDepositInterestReceipt receipt = Record(60_000m, 60_000m);

        receipt.NetAmount.Should().Be(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_Rejects_Non_Positive_GrossAmount(decimal gross)
    {
        Action act = () => Record(gross, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_Rejects_Negative_Deduction()
    {
        Action act = () => Record(60_000m, -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_Rejects_Deduction_Greater_Than_Gross()
    {
        Action act = () => Record(60_000m, 60_001m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkReversed_Sets_Flag_And_ReversalPostingId()
    {
        FixedDepositInterestReceipt receipt = Record();
        LedgerPostingId reversalId = LedgerPostingId.New();

        receipt.MarkReversed(reversalId, Now);

        receipt.IsReversed.Should().BeTrue();
        receipt.ReversalPostingId.Should().Be(reversalId);
    }

    [Fact]
    public void MarkReversed_Throws_When_Already_Reversed()
    {
        FixedDepositInterestReceipt receipt = Record();
        receipt.MarkReversed(LedgerPostingId.New(), Now);

        Action act = () => receipt.MarkReversed(LedgerPostingId.New(), Now);

        act.Should().Throw<InvalidOperationException>();
    }
}
