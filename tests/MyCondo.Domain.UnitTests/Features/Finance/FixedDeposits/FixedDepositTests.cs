using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.FixedDeposits.Exceptions;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Domain.UnitTests.Features.Finance.FixedDeposits;

public class FixedDepositTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FinancialAccountId FundingAccountId = FinancialAccountId.New();

    private static FixedDeposit Place(decimal principal = 500_000m, string certificateNumber = "FD-001") =>
        FixedDeposit.Place(
            FixedDepositId.New(), TenantId, $"  {certificateNumber}  ", "  City Bank  ", " Gulshan Branch ",
            FundingAccountId, fundId: null, principal, 7.5m, InterestCalculationMethod.Simple,
            InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            expectedGrossInterest: null, expectedDeductionRatePercent: null, notes: " ARP main FD ",
            LedgerPostingId.New(), Now);

    [Fact]
    public void Place_Trims_Fields_And_Starts_Active()
    {
        FixedDeposit fd = Place();

        fd.CertificateNumber.Should().Be("FD-001");
        fd.BankName.Should().Be("City Bank");
        fd.BranchName.Should().Be("Gulshan Branch");
        fd.Notes.Should().Be("ARP main FD");
        fd.Status.Should().Be(FixedDepositStatus.Active);
        fd.Version.Should().Be(1);
        fd.PredecessorFixedDepositId.Should().BeNull();
        fd.PlacementPostingId.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Place_Rejects_Non_Positive_Principal(decimal principal)
    {
        Action act = () => Place(principal);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Place_Rejects_Negative_InterestRate()
    {
        Action act = () => FixedDeposit.Place(
            FixedDepositId.New(), TenantId, "FD-002", "City Bank", null, FundingAccountId, null, 100_000m,
            -1m, InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), null, null, null, LedgerPostingId.New(), Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Place_Rejects_MaturityDate_Not_After_StartDate()
    {
        Action act = () => FixedDeposit.Place(
            FixedDepositId.New(), TenantId, "FD-003", "City Bank", null, FundingAccountId, null, 100_000m,
            5m, InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly, new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1), null, null, null, LedgerPostingId.New(), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateNotes_Changes_Notes_And_Bumps_Version()
    {
        FixedDeposit fd = Place();

        fd.UpdateNotes("revised notes", Now);

        fd.Notes.Should().Be("revised notes");
        fd.Version.Should().Be(2);
    }

    [Fact]
    public void MarkRenewed_Transitions_Active_To_Renewed_And_Links_Successor()
    {
        FixedDeposit predecessor = Place();
        FixedDepositId successorId = FixedDepositId.New();
        LedgerPostingId adjustmentId = LedgerPostingId.New();

        predecessor.MarkRenewed(successorId, adjustmentId, Now);

        predecessor.Status.Should().Be(FixedDepositStatus.Renewed);
        predecessor.SuccessorFixedDepositId.Should().Be(successorId);
        predecessor.RenewalAdjustmentPostingId.Should().Be(adjustmentId);
    }

    [Fact]
    public void MarkRenewed_Allows_Null_AdjustmentPostingId_For_Unchanged_Principal()
    {
        FixedDeposit predecessor = Place();

        predecessor.MarkRenewed(FixedDepositId.New(), null, Now);

        predecessor.RenewalAdjustmentPostingId.Should().BeNull();
    }

    [Fact]
    public void MarkRenewed_Throws_When_Not_Active()
    {
        FixedDeposit fd = Place();
        fd.MarkRenewed(FixedDepositId.New(), null, Now);

        Action act = () => fd.MarkRenewed(FixedDepositId.New(), null, Now);

        act.Should().Throw<FixedDepositInvalidStateTransitionException>();
    }

    [Fact]
    public void MarkWithdrawn_Transitions_Active_To_Withdrawn()
    {
        FixedDeposit fd = Place();
        LedgerPostingId withdrawalPostingId = LedgerPostingId.New();
        FinancialAccountId receivingAccountId = FinancialAccountId.New();

        fd.MarkWithdrawn(withdrawalPostingId, receivingAccountId, Now);

        fd.Status.Should().Be(FixedDepositStatus.Withdrawn);
        fd.WithdrawalPostingId.Should().Be(withdrawalPostingId);
        fd.ReceivingFinancialAccountId.Should().Be(receivingAccountId);
    }

    [Fact]
    public void MarkWithdrawn_Throws_When_Not_Active()
    {
        FixedDeposit fd = Place();
        fd.MarkWithdrawn(LedgerPostingId.New(), FinancialAccountId.New(), Now);

        Action act = () => fd.MarkWithdrawn(LedgerPostingId.New(), FinancialAccountId.New(), Now);

        act.Should().Throw<FixedDepositInvalidStateTransitionException>();
    }

    [Fact]
    public void Void_Transitions_Active_To_Voided_And_Stores_Reason()
    {
        FixedDeposit fd = Place();
        LedgerPostingId reversalId = LedgerPostingId.New();

        fd.Void("Data entry error", reversalId, Now);

        fd.Status.Should().Be(FixedDepositStatus.Voided);
        fd.VoidReason.Should().Be("Data entry error");
        fd.VoidReversalPostingId.Should().Be(reversalId);
    }

    [Fact]
    public void Void_Throws_When_Not_Active()
    {
        FixedDeposit fd = Place();
        fd.Void("First void", LedgerPostingId.New(), Now);

        Action act = () => fd.Void("Second void", LedgerPostingId.New(), Now);

        act.Should().Throw<FixedDepositInvalidStateTransitionException>();
    }

    [Fact]
    public void PlaceAsRenewal_Inherits_Predecessor_Bank_And_Fund_And_Links_Lineage()
    {
        FixedDeposit predecessor = Place();
        FixedDepositId successorId = FixedDepositId.New();

        FixedDeposit successor = FixedDeposit.PlaceAsRenewal(
            successorId, predecessor, "FD-001-R1", "New Branch", FundingAccountId, 550_000m, 8m,
            InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly, new DateOnly(2027, 1, 1),
            new DateOnly(2028, 1, 1), null, null, "renewed", LedgerPostingId.New(), Now);

        successor.BankName.Should().Be(predecessor.BankName);
        successor.FundId.Should().Be(predecessor.FundId);
        successor.PredecessorFixedDepositId.Should().Be(predecessor.Id);
        successor.CertificateNumber.Should().Be("FD-001-R1");
        successor.Principal.Should().Be(550_000m);
        successor.Status.Should().Be(FixedDepositStatus.Active);
    }

    [Fact]
    public void PlaceAsRenewal_Allows_Null_RenewalAdjustmentPostingId()
    {
        FixedDeposit predecessor = Place();

        FixedDeposit successor = FixedDeposit.PlaceAsRenewal(
            FixedDepositId.New(), predecessor, "FD-001-R1", null, FundingAccountId, predecessor.Principal,
            predecessor.InterestRatePercent, predecessor.CalculationMethod, predecessor.PaymentFrequency,
            new DateOnly(2027, 1, 1), new DateOnly(2028, 1, 1), null, null, null,
            renewalAdjustmentPostingId: null, Now);

        successor.PlacementPostingId.Should().BeNull();
    }
}
