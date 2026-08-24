using AwesomeAssertions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Notes;

/// <summary>
/// Task 5's reconciliation contract, exercised against a substituted
/// <see cref="IFinancialStatementNoteRepository"/>: every schedule must report the General Ledger figure
/// it explains, its own total, and the difference between them — never silently substituting one for the
/// other, and never hiding a gap. The repository's SQL behaviour (RLS, date/fund filtering, EF
/// translation) is proved separately by <c>FinancialStatementNoteQueryRlsTests</c> against real
/// PostgreSQL; here the concern is the attribution and arithmetic the service owns.
/// </summary>
public class FinancialStatementNoteServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly DateOnly AsOf = new(2026, 6, 30);
    private static readonly DateOnly PeriodStart = new(2026, 6, 1);

    private readonly IFinancialStatementNoteRepository _repository =
        Substitute.For<IFinancialStatementNoteRepository>();

    public FinancialStatementNoteServiceTests()
    {
        // Explicit empty defaults: an unstubbed NSubstitute call would hand back an auto-substituted
        // IReadOnlyList that is not safely enumerable.
        _repository.GetChartOfAccountsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetNoteAccountBalancesAsync(
                Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetNoteFlatBalancesAsync(
                Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetNotePostingReferenceBalancesAsync(
                Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetPostingReferencesAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetFinancialAccountRefsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetFixedDepositRefsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetFixedDepositInterestSourceRefsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetExpenseRefsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetFlatRefsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private FinancialStatementNoteService CreateService() => new(_repository);

    // -------------------------------------------------------------------------------------------
    // Fixtures
    // -------------------------------------------------------------------------------------------

    private static ChartOfAccount Account(
        string code, string name, AccountCategory category, LedgerDirection normal, FinancialStatementGroup group) =>
        ChartOfAccount.Create(TenantId, code, name, category, normal, statementGroup: group);

    private void SetAccounts(params ChartOfAccount[] accounts) =>
        _repository.GetChartOfAccountsAsync(TenantId, Arg.Any<CancellationToken>()).Returns(accounts);

    private void SetAccountBalances(ChartOfAccount account, decimal debit, decimal credit) =>
        _repository.GetNoteAccountBalancesAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(account.Id.Value)),
                Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([new NoteAccountBalanceLine(account.Id, debit, credit)]);

    private void SetFlatBalances(ChartOfAccount account, params NoteFlatBalanceLine[] lines) =>
        _repository.GetNoteFlatBalancesAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(account.Id.Value)),
                Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(lines);

    private void SetReferenceBalances(ChartOfAccount account, params NotePostingReferenceLine[] lines) =>
        _repository.GetNotePostingReferenceBalancesAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(account.Id.Value)),
                Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(lines);

    private async Task<FinancialStatementNote> AsOfNoteAsync(FinancialStatementNoteKey key, Guid? fundId = null) =>
        (await CreateService().GetAsOfNotesAsync(TenantId, AsOf, fundId, [key], CancellationToken.None)).Single();

    private async Task<FinancialStatementNote> PeriodNoteAsync(FinancialStatementNoteKey key, Guid? fundId = null) =>
        (await CreateService().GetPeriodNotesAsync(TenantId, PeriodStart, AsOf, fundId, [key], CancellationToken.None))
            .Single();

    // -------------------------------------------------------------------------------------------
    // Note selection / scope
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task As_Of_Request_Returns_Only_Balance_Sheet_Schedules()
    {
        IReadOnlyList<FinancialStatementNote> notes =
            await CreateService().GetAsOfNotesAsync(TenantId, AsOf, null, [], CancellationToken.None);

        notes.Select(n => n.Key).Should().BeEquivalentTo(IFinancialStatementNoteService.AsOfNoteKeys);
        notes.Should().OnlyContain(n => n.Scope == FinancialStatementNoteScope.AsOfDate && n.StartDate == null);
    }

    [Fact]
    public async Task Period_Request_Returns_Only_Income_And_Expenditure_Schedules()
    {
        IReadOnlyList<FinancialStatementNote> notes = await CreateService()
            .GetPeriodNotesAsync(TenantId, PeriodStart, AsOf, null, [], CancellationToken.None);

        notes.Select(n => n.Key).Should().BeEquivalentTo(IFinancialStatementNoteService.PeriodNoteKeys);
        notes.Should().OnlyContain(n => n.Scope == FinancialStatementNoteScope.Period && n.StartDate == PeriodStart);
    }

    [Fact]
    public async Task Requesting_A_Period_Schedule_From_The_As_Of_Endpoint_Returns_Nothing()
    {
        IReadOnlyList<FinancialStatementNote> notes = await CreateService().GetAsOfNotesAsync(
            TenantId, AsOf, null, [FinancialStatementNoteKey.OperatingExpensesByCategory], CancellationToken.None);

        notes.Should().BeEmpty("a period schedule cannot be rendered against an as-of-date window");
    }

    [Fact]
    public async Task Period_With_EndDate_Before_StartDate_Is_Rejected()
    {
        Func<Task> act = async () => await CreateService()
            .GetPeriodNotesAsync(TenantId, AsOf, PeriodStart, null, [], CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // -------------------------------------------------------------------------------------------
    // Tenant isolation / windowing propagation
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Every_Query_Is_Scoped_To_The_Requested_Tenant()
    {
        ChartOfAccount cash = Account("1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank);
        SetAccounts(cash);

        await AsOfNoteAsync(FinancialStatementNoteKey.CashAndBank);

        await _repository.Received().GetChartOfAccountsAsync(TenantId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetChartOfAccountsAsync(OtherTenantId, Arg.Any<CancellationToken>());
        await _repository.Received().GetNoteAccountBalancesAsync(
            TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task As_Of_Schedules_Query_A_Cumulative_Window_Ending_On_The_Statement_Date()
    {
        SetAccounts(Account("1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank));

        await AsOfNoteAsync(FinancialStatementNoteKey.CashAndBank);

        await _repository.Received().GetNoteAccountBalancesAsync(
            TenantId, Arg.Any<IReadOnlyList<Guid>>(), null, AsOf, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Period_Schedules_Query_The_Inclusive_Start_To_End_Window()
    {
        SetAccounts(Account("5000", "Operating Expenses", AccountCategory.Expense, LedgerDirection.Debit,
            FinancialStatementGroup.OperatingExpenses));

        await PeriodNoteAsync(FinancialStatementNoteKey.OperatingExpensesByCategory);

        await _repository.Received().GetNoteAccountBalancesAsync(
            TenantId, Arg.Any<IReadOnlyList<Guid>>(), PeriodStart, AsOf, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fund_Filter_Is_Applied_To_Every_Underlying_Query()
    {
        Guid fundId = Guid.NewGuid();
        ChartOfAccount receivable = Account("1200", "Resident Receivable", AccountCategory.Asset,
            LedgerDirection.Debit, FinancialStatementGroup.Receivables);
        SetAccounts(receivable);

        FinancialStatementNote note =
            await AsOfNoteAsync(FinancialStatementNoteKey.ServiceChargeReceivable, fundId);

        note.FundId.Should().Be(fundId);
        await _repository.Received().GetNoteAccountBalancesAsync(
            TenantId, Arg.Any<IReadOnlyList<Guid>>(), null, AsOf, fundId, Arg.Any<CancellationToken>());
        await _repository.Received().GetNoteFlatBalancesAsync(
            TenantId, Arg.Any<IReadOnlyList<Guid>>(), null, AsOf, fundId, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetNoteFlatBalancesAsync(
            TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(),
            null, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------------------------
    // Cash & Bank
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Cash_And_Bank_Reports_One_Row_Per_Account_And_Reconciles_Exactly()
    {
        ChartOfAccount bank = Account("1001", "Bank – GL", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank);
        ChartOfAccount petty = Account("1002", "Petty Cash – GL", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank);
        SetAccounts(bank, petty);

        _repository.GetNoteAccountBalancesAsync(
                TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns([
                new NoteAccountBalanceLine(bank.Id, 500_000m, 120_000m),
                new NoteAccountBalanceLine(petty.Id, 25_000m, 5_000m),
            ]);

        _repository.GetFinancialAccountRefsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new NoteFinancialAccountRef(
                    FinancialAccountId.New(), bank.Id, "Operating Account", FinancialAccountType.Bank,
                    "City Bank", "Gulshan", "1234567890123", null, true),
                new NoteFinancialAccountRef(
                    FinancialAccountId.New(), petty.Id, "Petty Cash", FinancialAccountType.Cash,
                    null, null, null, null, true),
            ]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.CashAndBank);

        note.GlBalance.Should().Be(400_000m);
        note.ScheduleBalance.Should().Be(400_000m);
        note.Difference.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.CashAndBankRows.Should().HaveCount(2);
        note.CashAndBankRows!.Should().Contain(r => r.Name == "Operating Account" && r.Balance == 380_000m);
        note.CashAndBankRows!.Should().Contain(r => r.Name == "Petty Cash" && r.Balance == 20_000m);
    }

    [Fact]
    public async Task Cash_And_Bank_Masks_The_Account_Number()
    {
        ChartOfAccount bank = Account("1001", "Bank – GL", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank);
        SetAccounts(bank);
        SetAccountBalances(bank, 1_000m, 0m);

        _repository.GetFinancialAccountRefsAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns([
                new NoteFinancialAccountRef(
                    FinancialAccountId.New(), bank.Id, "Operating Account", FinancialAccountType.Bank,
                    "City Bank", null, "1234567890123", null, true),
            ]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.CashAndBank);

        note.CashAndBankRows!.Single().MaskedAccountNumber.Should().Be("****0123");
    }

    [Fact]
    public async Task Cash_And_Bank_Keeps_An_Unlinked_Ledger_Account_In_The_Total_And_Warns()
    {
        ChartOfAccount systemCash = Account("1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank);
        SetAccounts(systemCash);
        SetAccountBalances(systemCash, 90_000m, 10_000m);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.CashAndBank);

        note.GlBalance.Should().Be(80_000m);
        note.ScheduleBalance.Should().Be(80_000m);
        note.IsReconciled.Should().BeTrue("an unlinked general-ledger cash account is still a real cash balance");
        note.CashAndBankRows!.Single().IsUnlinkedGlAccount.Should().BeTrue();
        note.Warnings.Should().Contain(w => w.Contains("not owned by a Financial Account"));
    }

    [Fact]
    public async Task Cash_And_Bank_With_No_Ledger_Activity_Reports_Zero_And_Reconciles()
    {
        SetAccounts(Account("1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.CashAndBank));

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.CashAndBank);

        note.GlBalance.Should().Be(0m);
        note.ScheduleBalance.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.CashAndBankRows.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------------------------
    // Fixed Deposits
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Fixed_Deposits_Report_Per_Certificate_Carrying_Balance_Separately_From_Accrued_Interest()
    {
        ChartOfAccount fdAccount = Account("1100", "Fixed Deposits", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.InvestmentsAndFixedDeposits);
        ChartOfAccount accruedAccount = Account("1150", "Interest Receivable", AccountCategory.Asset,
            LedgerDirection.Debit, FinancialStatementGroup.AccruedInterestReceivable);
        SetAccounts(fdAccount, accruedAccount);

        Guid activeDepositId = Guid.NewGuid();
        Guid maturedDepositId = Guid.NewGuid();
        Guid accrualId = Guid.NewGuid();

        SetAccountBalances(fdAccount, 1_000_000m, 400_000m);
        SetReferenceBalances(fdAccount,
            new NotePostingReferenceLine("FixedDepositPlacement", activeDepositId, 600_000m, 0m),
            new NotePostingReferenceLine("FixedDepositPlacement", maturedDepositId, 400_000m, 0m),
            new NotePostingReferenceLine("FixedDepositMaturity", maturedDepositId, 0m, 400_000m));

        SetAccountBalances(accruedAccount, 15_000m, 0m);
        SetReferenceBalances(accruedAccount,
            new NotePostingReferenceLine("FixedDepositInterestAccrual", accrualId, 15_000m, 0m));

        _repository.GetFixedDepositInterestSourceRefsAsync(
                TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new NoteFixedDepositInterestSourceRef(
                accrualId, new FixedDepositId(activeDepositId), 15_000m, 0m, false)]);

        _repository.GetFixedDepositRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                new NoteFixedDepositRef(
                    new FixedDepositId(activeDepositId), "FD-001", "City Bank", "Gulshan",
                    new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 600_000m, 9.5m,
                    FixedDepositStatus.Active, null),
                new NoteFixedDepositRef(
                    new FixedDepositId(maturedDepositId), "FD-002", "Dutch Bangla", null,
                    new DateOnly(2025, 6, 1), new DateOnly(2026, 6, 1), 400_000m, 8.0m,
                    FixedDepositStatus.Withdrawn, null),
            ]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.FixedDeposits);

        note.GlBalance.Should().Be(600_000m);
        note.ScheduleBalance.Should().Be(600_000m);
        note.IsReconciled.Should().BeTrue();

        FixedDepositNoteRow active = note.FixedDepositRows!.Single(r => r.CertificateNumber == "FD-001");
        active.CarryingBalance.Should().Be(600_000m);
        active.AccruedInterestReceivable.Should().Be(15_000m, "accrued interest is reported but never added to principal");
        active.CurrentStatus.Should().Be(FixedDepositStatus.Active);

        note.FixedDepositRows.Should().NotContain(r => r.CertificateNumber == "FD-002",
            "a withdrawn deposit carries no principal on the statement date and drops out");
    }

    /// <summary>Renewal capitalization and partial-withdrawal postings now carry the Fixed Deposit they
    /// move principal for as their source reference (<c>RenewFixedDepositCommandHandler</c>), so the
    /// schedule attributes them like any other FD movement and reconciles exactly. Capitalization is
    /// sourced to the successor (it establishes the successor's higher principal); a partial withdrawal
    /// is sourced to the predecessor (it returns part of that deposit's principal).</summary>
    [Fact]
    public async Task Renewal_Principal_Movements_Are_Attributed_And_Reconcile()
    {
        ChartOfAccount fdAccount = Account("1100", "Fixed Deposits", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.InvestmentsAndFixedDeposits);
        SetAccounts(fdAccount);

        Guid predecessorId = Guid.NewGuid();
        Guid successorId = Guid.NewGuid();
        Guid withdrawnDepositId = Guid.NewGuid();

        // Predecessor placed at 500,000 then renewed with 30,000 of interest capitalized into the
        // successor; a second deposit placed at 200,000 took a 50,000 partial withdrawal at renewal.
        SetAccountBalances(fdAccount, 730_000m, 50_000m);
        SetReferenceBalances(fdAccount,
            new NotePostingReferenceLine("FixedDepositPlacement", predecessorId, 500_000m, 0m),
            new NotePostingReferenceLine("FixedDepositRenewalCapitalization", successorId, 30_000m, 0m),
            new NotePostingReferenceLine("FixedDepositPlacement", withdrawnDepositId, 200_000m, 0m),
            new NotePostingReferenceLine("FixedDepositRenewalPartialWithdrawal", withdrawnDepositId, 0m, 50_000m));

        _repository.GetFixedDepositRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                Deposit(predecessorId, "FD-001", 500_000m, FixedDepositStatus.Renewed),
                Deposit(successorId, "FD-001-R1", 530_000m, FixedDepositStatus.Active),
                Deposit(withdrawnDepositId, "FD-002", 150_000m, FixedDepositStatus.Renewed),
            ]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.FixedDeposits);

        note.GlBalance.Should().Be(680_000m);
        note.ScheduleBalance.Should().Be(680_000m);
        note.Difference.Should().Be(0m);
        note.UnattributedAmount.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.FixedDepositRows.Should().NotContain(r => r.IsUnattributed);

        note.FixedDepositRows!.Single(r => r.CertificateNumber == "FD-001-R1").CarryingBalance
            .Should().Be(30_000m, "the capitalization is the successor's own principal movement");
        note.FixedDepositRows!.Single(r => r.CertificateNumber == "FD-002").CarryingBalance
            .Should().Be(150_000m, "the partial withdrawal reduces the deposit it was taken from, never below zero");
        note.FixedDepositRows.Should().NotContain(r => r.CarryingBalance < 0m);
    }

    /// <summary>Historical renewal postings made before renewal traceability existed carry no source id
    /// at all, so their principal effect stays genuinely unexplained — reported as a difference rather
    /// than guessed at or absorbed into the other rows.</summary>
    [Fact]
    public async Task Fixed_Deposits_Surface_Untraceable_Principal_Activity_As_A_Real_Difference()
    {
        ChartOfAccount fdAccount = Account("1100", "Fixed Deposits", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.InvestmentsAndFixedDeposits);
        SetAccounts(fdAccount);

        Guid depositId = Guid.NewGuid();

        SetAccountBalances(fdAccount, 550_000m, 0m);
        SetReferenceBalances(fdAccount,
            new NotePostingReferenceLine("FixedDepositPlacement", depositId, 500_000m, 0m),
            // A pre-fix historical renewal capitalization carries no source id at all.
            new NotePostingReferenceLine("FixedDepositRenewalCapitalization", null, 50_000m, 0m));

        _repository.GetFixedDepositRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new NoteFixedDepositRef(
                new FixedDepositId(depositId), "FD-001", "City Bank", null,
                new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 500_000m, 9.5m,
                FixedDepositStatus.Active, null)]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.FixedDeposits);

        note.GlBalance.Should().Be(550_000m, "the general ledger figure is never reduced to what the schedule explains");
        note.ScheduleBalance.Should().Be(500_000m);
        note.Difference.Should().Be(50_000m);
        note.UnattributedAmount.Should().Be(50_000m);
        note.IsReconciled.Should().BeFalse();
        note.FixedDepositRows.Should().Contain(r => r.IsUnattributed && r.CarryingBalance == 50_000m);
        note.Warnings.Should().Contain(w => w.Contains("could not be attributed to an individual"));
    }

    [Fact]
    public async Task Fixed_Deposits_With_Ledger_Activity_But_No_Instrument_Records_Report_The_Whole_Balance_As_Unexplained()
    {
        ChartOfAccount fdAccount = Account("1100", "Fixed Deposits", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.InvestmentsAndFixedDeposits);
        SetAccounts(fdAccount);

        SetAccountBalances(fdAccount, 200_000m, 0m);
        SetReferenceBalances(fdAccount,
            new NotePostingReferenceLine("FixedDepositPlacement", Guid.NewGuid(), 200_000m, 0m));

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.FixedDeposits);

        note.GlBalance.Should().Be(200_000m);
        note.ScheduleBalance.Should().Be(0m);
        note.Difference.Should().Be(200_000m);
        note.IsReconciled.Should().BeFalse();
    }

    // -------------------------------------------------------------------------------------------
    // Receivables / Advances (per-flat)
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Receivables_Report_Outstanding_Balance_Per_Flat_And_Reconcile()
    {
        ChartOfAccount receivable = Account("1200", "Resident Receivable", AccountCategory.Asset,
            LedgerDirection.Debit, FinancialStatementGroup.Receivables);
        SetAccounts(receivable);

        Guid flatA = Guid.NewGuid();
        Guid flatB = Guid.NewGuid();

        SetAccountBalances(receivable, 50_000m, 32_000m);
        SetFlatBalances(receivable,
            // Flat A: billed 30,000, allocations settled 12,000 → 18,000 outstanding.
            new NoteFlatBalanceLine(new FlatId(flatA), 30_000m, 12_000m),
            new NoteFlatBalanceLine(new FlatId(flatB), 20_000m, 20_000m));

        _repository.GetFlatRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new NoteFlatRef(new FlatId(flatA), "A-101", Guid.NewGuid(), "Tower A")]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.ServiceChargeReceivable);

        note.GlBalance.Should().Be(18_000m);
        note.ScheduleBalance.Should().Be(18_000m);
        note.IsReconciled.Should().BeTrue();

        note.FlatBalanceRows.Should().ContainSingle();
        FlatBalanceNoteRow row = note.FlatBalanceRows!.Single();
        row.FlatId.Should().Be(flatA);
        row.FlatNumber.Should().Be("A-101");
        row.BuildingName.Should().Be("Tower A");
        row.Balance.Should().Be(18_000m);
    }

    [Fact]
    public async Task Receivables_Document_That_Service_Charge_And_Additional_Charges_Are_Not_Separable()
    {
        SetAccounts(Account("1200", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit,
            FinancialStatementGroup.Receivables));

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.ServiceChargeReceivable);

        note.Warnings.Should().Contain(w => w.Contains("Additional Charge"));
    }

    [Fact]
    public async Task Receivable_Ledger_Activity_Without_A_Flat_Becomes_A_Visible_Difference()
    {
        ChartOfAccount receivable = Account("1200", "Resident Receivable", AccountCategory.Asset,
            LedgerDirection.Debit, FinancialStatementGroup.Receivables);
        SetAccounts(receivable);

        SetAccountBalances(receivable, 12_000m, 0m);
        SetFlatBalances(receivable, new NoteFlatBalanceLine(null, 12_000m, 0m));

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.ServiceChargeReceivable);

        note.GlBalance.Should().Be(12_000m);
        note.ScheduleBalance.Should().Be(0m);
        note.Difference.Should().Be(12_000m);
        note.IsReconciled.Should().BeFalse();
        note.FlatBalanceRows.Should().ContainSingle(r => r.IsUnattributed);
    }

    [Fact]
    public async Task Resident_Advances_Are_Signed_As_A_Liability()
    {
        ChartOfAccount advances = Account("2100", "Resident Advances", AccountCategory.Liability,
            LedgerDirection.Credit, FinancialStatementGroup.ResidentAdvances);
        SetAccounts(advances);

        Guid flatId = Guid.NewGuid();

        // Advance received 10,000; 4,000 later applied against an invoice → 6,000 still held.
        SetAccountBalances(advances, 4_000m, 10_000m);
        SetFlatBalances(advances, new NoteFlatBalanceLine(new FlatId(flatId), 4_000m, 10_000m));

        _repository.GetFlatRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new NoteFlatRef(new FlatId(flatId), "B-202", Guid.NewGuid(), "Tower B")]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.ResidentAdvances);

        note.GlBalance.Should().Be(6_000m);
        note.ScheduleBalance.Should().Be(6_000m);
        note.IsReconciled.Should().BeTrue();
        note.FlatBalanceRows!.Single().Balance.Should().Be(6_000m);
    }

    [Fact]
    public async Task A_Fully_Consumed_Advance_Drops_Out_Of_The_Schedule()
    {
        ChartOfAccount advances = Account("2100", "Resident Advances", AccountCategory.Liability,
            LedgerDirection.Credit, FinancialStatementGroup.ResidentAdvances);
        SetAccounts(advances);

        SetFlatBalances(advances, new NoteFlatBalanceLine(new FlatId(Guid.NewGuid()), 10_000m, 10_000m));

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.ResidentAdvances);

        note.GlBalance.Should().Be(0m);
        note.ScheduleBalance.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.FlatBalanceRows.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------------------------
    // Payables
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Payables_Report_Only_Still_Unpaid_Expenses()
    {
        ChartOfAccount payable = Account("2000", "Accounts Payable", AccountCategory.Liability,
            LedgerDirection.Credit, FinancialStatementGroup.AccountsPayable);
        SetAccounts(payable);

        Guid unpaidExpenseId = Guid.NewGuid();
        Guid paidExpenseId = Guid.NewGuid();

        SetAccountBalances(payable, 40_000m, 100_000m);
        SetReferenceBalances(payable,
            new NotePostingReferenceLine("ExpenseRecording", unpaidExpenseId, 0m, 60_000m),
            new NotePostingReferenceLine("ExpenseRecording", paidExpenseId, 0m, 40_000m),
            new NotePostingReferenceLine("ExpensePayment", paidExpenseId, 40_000m, 0m));

        _repository.GetExpenseRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                ExpenseRef(unpaidExpenseId, "Lift AMC", 60_000m, ExpenseStatus.Posted, "Maintenance"),
                ExpenseRef(paidExpenseId, "Generator fuel", 40_000m, ExpenseStatus.Paid, "Utilities"),
            ]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.Payables);

        note.GlBalance.Should().Be(60_000m);
        note.ScheduleBalance.Should().Be(60_000m);
        note.IsReconciled.Should().BeTrue();
        note.PayableRows.Should().ContainSingle();
        note.PayableRows!.Single().Description.Should().Be("Lift AMC");
        note.PayableRows!.Single().OutstandingAmount.Should().Be(60_000m);
    }

    [Fact]
    public async Task A_Voided_Expense_Nets_Out_Of_Payables_Through_The_Reversal_Posting_Reference()
    {
        ChartOfAccount payable = Account("2000", "Accounts Payable", AccountCategory.Liability,
            LedgerDirection.Credit, FinancialStatementGroup.AccountsPayable);
        SetAccounts(payable);

        Guid expenseId = Guid.NewGuid();
        Guid originalPostingId = Guid.NewGuid();

        SetAccountBalances(payable, 25_000m, 25_000m);
        SetReferenceBalances(payable,
            new NotePostingReferenceLine("ExpenseRecording", expenseId, 0m, 25_000m),
            // A void posting references the ORIGINAL POSTING id, not the expense.
            new NotePostingReferenceLine("ExpenseVoid", originalPostingId, 25_000m, 0m));

        _repository.GetExpenseRefsAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(expenseId)), Arg.Any<CancellationToken>())
            .Returns([ExpenseRef(expenseId, "Cancelled order", 25_000m, ExpenseStatus.Voided, "Maintenance")]);

        _repository.GetPostingReferencesAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(originalPostingId)),
                Arg.Any<CancellationToken>())
            .Returns([new NotePostingReference(new LedgerPostingId(originalPostingId), "ExpenseRecording", expenseId)]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.Payables);

        note.GlBalance.Should().Be(0m);
        note.ScheduleBalance.Should().Be(0m);
        note.Difference.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.PayableRows.Should().BeEmpty("a voided expense's payable is fully reversed");
    }

    [Fact]
    public async Task Payable_Activity_With_No_Traceable_Expense_Is_Reported_As_A_Difference()
    {
        ChartOfAccount payable = Account("2000", "Accounts Payable", AccountCategory.Liability,
            LedgerDirection.Credit, FinancialStatementGroup.AccountsPayable);
        SetAccounts(payable);

        SetAccountBalances(payable, 0m, 15_000m);
        SetReferenceBalances(payable, new NotePostingReferenceLine("SomethingElse", null, 0m, 15_000m));

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.Payables);

        note.GlBalance.Should().Be(15_000m);
        note.ScheduleBalance.Should().Be(0m);
        note.Difference.Should().Be(15_000m);
        note.UnattributedAmount.Should().Be(15_000m);
        note.IsReconciled.Should().BeFalse();
        note.PayableRows.Should().ContainSingle(r => r.IsUnattributed);
    }

    // -------------------------------------------------------------------------------------------
    // Interest Income
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Interest_Income_Separates_Recognized_Income_From_Interest_Received()
    {
        ChartOfAccount interestIncome = Account("4400", "FD Interest Income", AccountCategory.Income,
            LedgerDirection.Credit, FinancialStatementGroup.InterestIncome);
        ChartOfAccount interestReceivable = Account("1150", "Interest Receivable", AccountCategory.Asset,
            LedgerDirection.Debit, FinancialStatementGroup.AccruedInterestReceivable);
        SetAccounts(interestIncome, interestReceivable);

        Guid depositId = Guid.NewGuid();
        Guid accrualId = Guid.NewGuid();
        Guid receiptId = Guid.NewGuid();

        SetAccountBalances(interestIncome, 0m, 30_000m);
        SetReferenceBalances(interestIncome,
            new NotePostingReferenceLine("FixedDepositInterestAccrual", accrualId, 0m, 30_000m));

        SetReferenceBalances(interestReceivable,
            new NotePostingReferenceLine("FixedDepositInterestReceipt", receiptId, 0m, 20_000m));

        _repository.GetFixedDepositInterestSourceRefsAsync(
                TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                new NoteFixedDepositInterestSourceRef(accrualId, new FixedDepositId(depositId), 30_000m, 0m, false),
                new NoteFixedDepositInterestSourceRef(receiptId, new FixedDepositId(depositId), 20_000m, 2_000m, true),
            ]);

        _repository.GetFixedDepositRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new NoteFixedDepositRef(
                new FixedDepositId(depositId), "FD-001", "City Bank", null,
                new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), 600_000m, 9.5m,
                FixedDepositStatus.Active, null)]);

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.InterestIncome);

        note.GlBalance.Should().Be(30_000m);
        note.ScheduleBalance.Should().Be(30_000m);
        note.IsReconciled.Should().BeTrue();

        InterestIncomeNoteRow row = note.InterestIncomeRows!.Single();
        row.CertificateNumber.Should().Be("FD-001");
        row.InterestRecognized.Should().Be(30_000m);
        row.InterestReceivedGross.Should().Be(20_000m);
        row.InterestDeducted.Should().Be(2_000m);
    }

    [Fact]
    public async Task Interest_Income_That_Is_Not_Fixed_Deposit_Interest_Becomes_A_Difference()
    {
        ChartOfAccount interestIncome = Account("4400", "Interest Income", AccountCategory.Income,
            LedgerDirection.Credit, FinancialStatementGroup.InterestIncome);
        SetAccounts(interestIncome);

        SetAccountBalances(interestIncome, 0m, 5_000m);
        SetReferenceBalances(interestIncome, new NotePostingReferenceLine("SavingsInterest", null, 0m, 5_000m));

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.InterestIncome);

        note.GlBalance.Should().Be(5_000m);
        note.ScheduleBalance.Should().Be(0m);
        note.Difference.Should().Be(5_000m);
        note.IsReconciled.Should().BeFalse();
    }

    // -------------------------------------------------------------------------------------------
    // Operating Expenses by category
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Operating_Expenses_Break_The_Single_Ledger_Total_Down_By_Category_And_Reconcile()
    {
        ChartOfAccount operating = Account("5000", "Operating Expenses", AccountCategory.Expense,
            LedgerDirection.Debit, FinancialStatementGroup.OperatingExpenses);
        SetAccounts(operating);

        Guid liftExpenseId = Guid.NewGuid();
        Guid fuelExpenseId = Guid.NewGuid();
        Guid guardExpenseId = Guid.NewGuid();
        ExpenseCategoryId maintenance = ExpenseCategoryId.New();
        ExpenseCategoryId security = ExpenseCategoryId.New();

        SetAccountBalances(operating, 145_000m, 0m);
        SetReferenceBalances(operating,
            new NotePostingReferenceLine("ExpenseRecording", liftExpenseId, 60_000m, 0m),
            new NotePostingReferenceLine("ExpenseRecording", fuelExpenseId, 40_000m, 0m),
            new NotePostingReferenceLine("ExpenseRecording", guardExpenseId, 45_000m, 0m));

        _repository.GetExpenseRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([
                ExpenseRef(liftExpenseId, "Lift AMC", 60_000m, ExpenseStatus.Posted, "Maintenance", maintenance),
                ExpenseRef(fuelExpenseId, "Generator fuel", 40_000m, ExpenseStatus.Posted, "Maintenance", maintenance),
                ExpenseRef(guardExpenseId, "Guard salary", 45_000m, ExpenseStatus.Paid, "Security", security),
            ]);

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.OperatingExpensesByCategory);

        note.GlBalance.Should().Be(145_000m);
        note.ScheduleBalance.Should().Be(145_000m);
        note.Difference.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();

        note.OperatingExpenseCategoryRows.Should().HaveCount(2);
        note.OperatingExpenseCategoryRows!.Should().Contain(r =>
            r.ExpenseCategoryId == maintenance.Value && r.Amount == 100_000m && r.ExpenseCount == 2);
        note.OperatingExpenseCategoryRows!.Should().Contain(r =>
            r.ExpenseCategoryId == security.Value && r.Amount == 45_000m && r.ExpenseCount == 1);
    }

    [Fact]
    public async Task A_Voided_Expense_Nets_To_Zero_In_The_Category_Breakdown()
    {
        ChartOfAccount operating = Account("5000", "Operating Expenses", AccountCategory.Expense,
            LedgerDirection.Debit, FinancialStatementGroup.OperatingExpenses);
        SetAccounts(operating);

        Guid expenseId = Guid.NewGuid();
        Guid originalPostingId = Guid.NewGuid();

        SetAccountBalances(operating, 30_000m, 30_000m);
        SetReferenceBalances(operating,
            new NotePostingReferenceLine("ExpenseRecording", expenseId, 30_000m, 0m),
            new NotePostingReferenceLine("ExpenseVoid", originalPostingId, 0m, 30_000m));

        _repository.GetExpenseRefsAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(expenseId)), Arg.Any<CancellationToken>())
            .Returns([ExpenseRef(expenseId, "Cancelled", 30_000m, ExpenseStatus.Voided, "Maintenance")]);

        _repository.GetPostingReferencesAsync(
                TenantId, Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(originalPostingId)),
                Arg.Any<CancellationToken>())
            .Returns([new NotePostingReference(new LedgerPostingId(originalPostingId), "ExpenseRecording", expenseId)]);

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.OperatingExpensesByCategory);

        note.GlBalance.Should().Be(0m);
        note.ScheduleBalance.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.OperatingExpenseCategoryRows.Should().BeEmpty();
    }

    [Fact]
    public async Task An_Expense_Type_With_No_Category_Is_Grouped_As_Uncategorised()
    {
        ChartOfAccount operating = Account("5000", "Operating Expenses", AccountCategory.Expense,
            LedgerDirection.Debit, FinancialStatementGroup.OperatingExpenses);
        SetAccounts(operating);

        Guid expenseId = Guid.NewGuid();
        SetAccountBalances(operating, 12_000m, 0m);
        SetReferenceBalances(operating, new NotePostingReferenceLine("ExpenseRecording", expenseId, 12_000m, 0m));

        _repository.GetExpenseRefsAsync(TenantId, Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new NoteExpenseRef(
                new ExpenseId(expenseId), "Legacy expense", null, null, new DateOnly(2026, 6, 10), 12_000m,
                ExpenseStatus.Posted, null, null, "Legacy type")]);

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.OperatingExpensesByCategory);

        note.IsReconciled.Should().BeTrue();
        note.OperatingExpenseCategoryRows!.Single().ExpenseCategoryId.Should().BeNull();
        note.OperatingExpenseCategoryRows!.Single().CategoryName.Should().Be("Uncategorised");
        note.Warnings.Should().Contain(w => w.Contains("Uncategorised"));
    }

    [Fact]
    public async Task Operating_Expense_Ledger_Activity_With_No_Source_Expense_Is_Reported_As_Unexplained()
    {
        ChartOfAccount operating = Account("5000", "Operating Expenses", AccountCategory.Expense,
            LedgerDirection.Debit, FinancialStatementGroup.OperatingExpenses);
        SetAccounts(operating);

        SetAccountBalances(operating, 9_000m, 0m);
        SetReferenceBalances(operating, new NotePostingReferenceLine("ManualJournal", null, 9_000m, 0m));

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.OperatingExpensesByCategory);

        note.GlBalance.Should().Be(9_000m);
        note.ScheduleBalance.Should().Be(0m);
        note.Difference.Should().Be(9_000m);
        note.IsReconciled.Should().BeFalse();
        note.OperatingExpenseCategoryRows.Should().ContainSingle(r => r.IsUnattributed && r.Amount == 9_000m);
        note.Warnings.Should().Contain(w => w.Contains("statement figure remains the general-ledger total"));
    }

    [Fact]
    public async Task Operating_Expenses_With_No_Ledger_Activity_Report_Zero()
    {
        SetAccounts(Account("5000", "Operating Expenses", AccountCategory.Expense, LedgerDirection.Debit,
            FinancialStatementGroup.OperatingExpenses));

        FinancialStatementNote note = await PeriodNoteAsync(FinancialStatementNoteKey.OperatingExpensesByCategory);

        note.GlBalance.Should().Be(0m);
        note.ScheduleBalance.Should().Be(0m);
        note.IsReconciled.Should().BeTrue();
        note.OperatingExpenseCategoryRows.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------------------------
    // Detail truncation
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Detail_Rows_Are_Capped_While_Totals_Stay_Complete()
    {
        ChartOfAccount receivable = Account("1200", "Resident Receivable", AccountCategory.Asset,
            LedgerDirection.Debit, FinancialStatementGroup.Receivables);
        SetAccounts(receivable);

        const int flatCount = FinancialStatementNoteService.MaxDetailRows + 25;
        List<NoteFlatBalanceLine> lines = Enumerable.Range(0, flatCount)
            .Select(_ => new NoteFlatBalanceLine(new FlatId(Guid.NewGuid()), 1_000m, 0m))
            .ToList();

        SetAccountBalances(receivable, flatCount * 1_000m, 0m);
        SetFlatBalances(receivable, [.. lines]);

        FinancialStatementNote note = await AsOfNoteAsync(FinancialStatementNoteKey.ServiceChargeReceivable);

        note.TotalRowCount.Should().Be(flatCount);
        note.IsDetailTruncated.Should().BeTrue();
        note.FlatBalanceRows.Should().HaveCount(FinancialStatementNoteService.MaxDetailRows);
        note.ScheduleBalance.Should().Be(flatCount * 1_000m, "truncation limits the payload, never the arithmetic");
        note.IsReconciled.Should().BeTrue();
    }

    private static NoteFixedDepositRef Deposit(
        Guid depositId, string certificateNumber, decimal principal, FixedDepositStatus status) =>
        new(new FixedDepositId(depositId), certificateNumber, "City Bank", null,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), principal, 9.5m, status, null);

    private static NoteExpenseRef ExpenseRef(
        Guid expenseId, string description, decimal amount, ExpenseStatus status, string categoryName,
        ExpenseCategoryId? categoryId = null) =>
        new(new ExpenseId(expenseId), description, "Acme Ltd", "INV-1", new DateOnly(2026, 6, 10), amount,
            status, categoryId ?? ExpenseCategoryId.New(), categoryName, $"{categoryName} type");
}
