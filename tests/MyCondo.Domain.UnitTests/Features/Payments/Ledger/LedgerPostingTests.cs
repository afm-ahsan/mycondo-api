using AwesomeAssertions;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Ledger.Exceptions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Payments.Ledger;

public class LedgerPostingTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateOnly BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Create_Produces_Balanced_Posting_And_Entries()
    {
        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 500m, "Payment"),
            new LedgerLine(LedgerAccountType.ResidentReceivable, FlatId, LedgerDirection.Credit, 500m, "Payment"),
        ];

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            TenantId, BusinessDate, "Payment", "Payment", null, lines, Now);

        posting.TenantId.Should().Be(TenantId);
        posting.BusinessDate.Should().Be(BusinessDate);
        entries.Should().HaveCount(2);
        entries.Should().OnlyContain(e => e.PostingId == posting.Id);
        entries.Sum(e => e.Direction == LedgerDirection.Debit ? e.Amount : -e.Amount).Should().Be(0m);
    }

    [Fact]
    public void Create_Throws_When_Debits_And_Credits_Do_Not_Balance()
    {
        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 500m, "Payment"),
            new LedgerLine(LedgerAccountType.ResidentReceivable, FlatId, LedgerDirection.Credit, 400m, "Payment"),
        ];

        Action act = () => LedgerPosting.Create(TenantId, BusinessDate, "Payment", null, null, lines, Now);

        act.Should().Throw<LedgerPostingUnbalancedException>();
    }

    [Fact]
    public void Create_Throws_When_Fewer_Than_Two_Lines()
    {
        LedgerLine[] lines = [new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 500m, "x")];

        Action act = () => LedgerPosting.Create(TenantId, BusinessDate, "x", null, null, lines, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_When_ResidentReceivable_Line_Has_No_FlatId()
    {
        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 500m, "x"),
            new LedgerLine(LedgerAccountType.ResidentReceivable, null, LedgerDirection.Credit, 500m, "x"),
        ];

        Action act = () => LedgerPosting.Create(TenantId, BusinessDate, "x", null, null, lines, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_When_NonResidentReceivable_Line_Has_A_FlatId()
    {
        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, FlatId, LedgerDirection.Debit, 500m, "x"),
            new LedgerLine(LedgerAccountType.ResidentReceivable, FlatId, LedgerDirection.Credit, 500m, "x"),
        ];

        Action act = () => LedgerPosting.Create(TenantId, BusinessDate, "x", null, null, lines, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_When_Line_Amount_Is_Not_Positive()
    {
        LedgerLine[] lines =
        [
            new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 0m, "x"),
            new LedgerLine(LedgerAccountType.ResidentReceivable, FlatId, LedgerDirection.Credit, 0m, "x"),
        ];

        Action act = () => LedgerPosting.Create(TenantId, BusinessDate, "x", null, null, lines, Now);

        act.Should().Throw<ArgumentException>();
    }
}
