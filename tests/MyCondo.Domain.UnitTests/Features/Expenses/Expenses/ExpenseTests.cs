using AwesomeAssertions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.Expenses.Exceptions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.UnitTests.Features.Expenses.Expenses;

public class ExpenseTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly ExpenseTypeId ExpenseTypeId = ExpenseTypeId.New();

    private static Expense Record(decimal amount = 1000m, bool isPaid = false) =>
        Expense.Record(
            TenantId, BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 1),
            accountingDate: null, "  Monthly cleaning  ", " ABC Cleaners ", " INV-001 ", amount, isPaid,
            PaymentMethod.Cash, " paid in full ", Now);

    [Fact]
    public void Record_Trims_Text_Fields_And_Starts_Recorded()
    {
        Expense expense = Record();

        expense.Description.Should().Be("Monthly cleaning");
        expense.Payee.Should().Be("ABC Cleaners");
        expense.ReferenceNumber.Should().Be("INV-001");
        expense.Notes.Should().Be("paid in full");
        expense.Status.Should().Be(ExpenseStatus.Recorded);
        expense.Version.Should().Be(1);
        expense.AccountingDate.Should().Be(expense.ExpenseDate);
    }

    [Fact]
    public void Record_Allows_Null_BuildingId_For_Association_Wide_Common_Expense()
    {
        Expense expense = Expense.Record(
            TenantId, buildingId: null, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 1),
            accountingDate: null, "Common area expense", null, null, 500m, isPaid: false, PaymentMethod.Cash, null, Now);

        expense.BuildingId.Should().BeNull();
    }

    [Fact]
    public void Record_Defaults_AccountingDate_To_ExpenseDate_When_Not_Supplied()
    {
        Expense expense = Expense.Record(
            TenantId, BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 1), accountingDate: null,
            "Desc", null, null, 100m, isPaid: false, PaymentMethod.Cash, null, Now);

        expense.AccountingDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void Record_Uses_Explicit_AccountingDate_When_Supplied()
    {
        Expense expense = Expense.Record(
            TenantId, BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 5), "Desc", null, null, 100m, isPaid: false, PaymentMethod.Cash, null, Now);

        expense.AccountingDate.Should().Be(new DateOnly(2026, 8, 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_Rejects_Non_Positive_Amounts(decimal amount)
    {
        Action act = () => Record(amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_Changes_Fields_And_Bumps_Version()
    {
        Expense expense = Record();

        expense.Update(
            BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 2), accountingDate: null,
            "Corrected description", "New Payee", "INV-002", 2000m, isPaid: true, PaymentMethod.BankTransfer,
            "updated", DateTimeOffset.UtcNow);

        expense.Description.Should().Be("Corrected description");
        expense.Amount.Should().Be(2000m);
        expense.PaymentMethod.Should().Be(PaymentMethod.BankTransfer);
        expense.IsPaid.Should().BeTrue();
        expense.Version.Should().Be(2);
    }

    [Fact]
    public void Update_Throws_When_Amount_Is_Not_Positive()
    {
        Expense expense = Record();

        Action act = () => expense.Update(
            BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 2), accountingDate: null, "Desc", null,
            null, 0m, isPaid: false, PaymentMethod.Cash, null, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Void_Sets_Status_And_Reason_When_Never_Posted()
    {
        Expense expense = Record();

        expense.Void("Duplicate entry", DateTimeOffset.UtcNow);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.VoidReason.Should().Be("Duplicate entry");
        expense.ReversalPostingId.Should().BeNull();
    }

    [Fact]
    public void Void_Is_Idempotent_For_An_Already_Voided_Expense()
    {
        Expense expense = Record();
        expense.Void("First reason", DateTimeOffset.UtcNow);
        int versionAfterFirstVoid = expense.Version;

        expense.Void("Second reason", DateTimeOffset.UtcNow);

        expense.VoidReason.Should().Be("First reason");
        expense.Version.Should().Be(versionAfterFirstVoid);
    }

    [Fact]
    public void Update_Throws_When_Expense_Is_Already_Voided()
    {
        Expense expense = Record();
        expense.Void("Reason", DateTimeOffset.UtcNow);

        Action act = () => expense.Update(
            BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 2), accountingDate: null, "Desc", null,
            null, 500m, isPaid: false, PaymentMethod.Cash, null, DateTimeOffset.UtcNow);

        act.Should().Throw<ExpenseNotEditableException>();
    }

    [Fact]
    public void MarkPosted_Transitions_Recorded_To_Posted_And_Stamps_PostingId()
    {
        Expense expense = Record();
        LedgerPostingId postingId = LedgerPostingId.New();
        ChartOfAccountId accountId = ChartOfAccountId.New();

        expense.MarkPosted(postingId, accountId, DateTimeOffset.UtcNow);

        expense.Status.Should().Be(ExpenseStatus.Posted);
        expense.PostingId.Should().Be(postingId);
        expense.FinancialAccountId.Should().Be(accountId);
        expense.Version.Should().Be(2);
    }

    [Fact]
    public void MarkPosted_Throws_When_Not_Recorded()
    {
        Expense expense = Record();
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);

        Action act = () => expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);

        act.Should().Throw<ExpenseInvalidStateTransitionException>();
    }

    [Fact]
    public void Update_Throws_Once_Posted()
    {
        Expense expense = Record();
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);

        Action act = () => expense.Update(
            BuildingId, ExpenseTypeId, fundId: null, new DateOnly(2026, 8, 2), accountingDate: null, "Desc", null,
            null, 500m, isPaid: false, PaymentMethod.Cash, null, DateTimeOffset.UtcNow);

        act.Should().Throw<ExpenseNotEditableException>();
    }

    [Fact]
    public void MarkPaid_Transitions_Posted_Unpaid_To_Paid()
    {
        Expense expense = Record(isPaid: false);
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);
        LedgerPostingId paymentPostingId = LedgerPostingId.New();
        ChartOfAccountId bankAccountId = ChartOfAccountId.New();

        expense.MarkPaid(paymentPostingId, bankAccountId, DateTimeOffset.UtcNow);

        expense.Status.Should().Be(ExpenseStatus.Paid);
        expense.IsPaid.Should().BeTrue();
        expense.PaymentPostingId.Should().Be(paymentPostingId);
        expense.FinancialAccountId.Should().Be(bankAccountId);
    }

    [Fact]
    public void MarkPaid_Throws_When_Already_Paid_At_Recording_Time()
    {
        Expense expense = Record(isPaid: true);
        expense.MarkPosted(LedgerPostingId.New(), ChartOfAccountId.New(), DateTimeOffset.UtcNow);

        Action act = () => expense.MarkPaid(LedgerPostingId.New(), ChartOfAccountId.New(), DateTimeOffset.UtcNow);

        act.Should().Throw<ExpenseInvalidStateTransitionException>();
    }

    [Fact]
    public void MarkPaid_Throws_When_Not_Yet_Posted()
    {
        Expense expense = Record(isPaid: false);

        Action act = () => expense.MarkPaid(LedgerPostingId.New(), ChartOfAccountId.New(), DateTimeOffset.UtcNow);

        act.Should().Throw<ExpenseInvalidStateTransitionException>();
    }

    [Fact]
    public void Void_Requires_ReversalPostingId_Once_Posted()
    {
        Expense expense = Record();
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);

        Action act = () => expense.Void("Correction", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Void_Stores_ReversalPostingId_When_Posted()
    {
        Expense expense = Record();
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);
        LedgerPostingId reversalId = LedgerPostingId.New();

        expense.Void("Correction", DateTimeOffset.UtcNow, reversalId);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.ReversalPostingId.Should().Be(reversalId);
    }

    [Fact]
    public void Void_Requires_PaymentReversalPostingId_Once_Paid()
    {
        Expense expense = Record(isPaid: false);
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);
        expense.MarkPaid(LedgerPostingId.New(), ChartOfAccountId.New(), DateTimeOffset.UtcNow);

        Action act = () => expense.Void("Correction", DateTimeOffset.UtcNow, LedgerPostingId.New());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Void_Stores_Both_Reversal_Ids_When_Paid()
    {
        Expense expense = Record(isPaid: false);
        expense.MarkPosted(LedgerPostingId.New(), null, DateTimeOffset.UtcNow);
        expense.MarkPaid(LedgerPostingId.New(), ChartOfAccountId.New(), DateTimeOffset.UtcNow);
        LedgerPostingId reversalId = LedgerPostingId.New();
        LedgerPostingId paymentReversalId = LedgerPostingId.New();

        expense.Void("Correction", DateTimeOffset.UtcNow, reversalId, paymentReversalId);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.ReversalPostingId.Should().Be(reversalId);
        expense.PaymentReversalPostingId.Should().Be(paymentReversalId);
    }
}
