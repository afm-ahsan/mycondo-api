using AwesomeAssertions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// CondoBD Finance Phase 2A Task 5's <see cref="FinancialStatementNoteRepository"/> introduces new EF
/// LINQ queries over the RLS-protected <c>finance</c>/<c>payments</c>/<c>expenses</c>/<c>property</c>
/// schemas — per-account, per-flat and per-posting-reference aggregations plus several drill-down
/// lookups. This suite proves, against real PostgreSQL and the restricted <c>mycondo_app</c> role, that
/// (a) every one of them actually translates to SQL, (b) none of them leak across tenants, (c) a
/// connection with no tenant context sees nothing, and (d) the as-of/period/fund windowing a supporting
/// schedule depends on behaves exactly like the primary statement's. Requires a Docker daemon — see
/// <see cref="MultiTenancyPostgresFixture"/>'s doc comment.
/// </summary>
public class FinancialStatementNoteQueryRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly MultiTenancyPostgresFixture _fixture;

    public FinancialStatementNoteQueryRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // -------------------------------------------------------------------------------------------
    // Per-account balances — tenant isolation, default deny, as-of/period windowing, fund scoping
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Account_Balances_Only_Return_The_Calling_Tenants_Activity()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        (Guid cashA, _) = await SeedCashPostingAsync(tenantA, 1_000m, asOf, fundId: null);
        (Guid cashB, _) = await SeedCashPostingAsync(tenantB, 500m, asOf, fundId: null);

        await using MyCondoDbContext dbAsTenantA = _fixture.CreateDbContext(tenantA);
        FinancialStatementNoteRepository repository = new(dbAsTenantA);

        IReadOnlyList<NoteAccountBalanceLine> lines = await repository.GetNoteAccountBalancesAsync(
            tenantA, [cashA, cashB], null, asOf, null, CancellationToken.None);

        lines.Should().ContainSingle();
        lines[0].TotalDebit.Should().Be(1_000m,
            "tenant B's cash account was named explicitly and must still return nothing under tenant A's context");
    }

    [Fact]
    public async Task Account_Balances_With_No_Tenant_Context_Return_Nothing()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        (Guid cashAccountId, _) = await SeedCashPostingAsync(tenantId, 1_000m, asOf, fundId: null);

        await using MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null);
        FinancialStatementNoteRepository repository = new(noTenant);

        IReadOnlyList<NoteAccountBalanceLine> lines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [cashAccountId], null, asOf, null, CancellationToken.None);

        lines.Should().BeEmpty("a connection with no tenant context must default-deny, not fall back to open access");
    }

    [Fact]
    public async Task An_As_Of_Schedule_Excludes_Activity_Dated_After_The_Statement_Date()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        (Guid cashAccountId, _) = await SeedCashPostingAsync(tenantId, 1_000m, new DateOnly(2026, 7, 1), fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteAccountBalanceLine> lines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [cashAccountId], null, asOf, null, CancellationToken.None);

        lines.Should().BeEmpty(
            "a supporting schedule must reconstruct the statement date, never drift forward to today's balance");
    }

    [Fact]
    public async Task An_As_Of_Schedule_Includes_Activity_Dated_Exactly_On_The_Statement_Date()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        (Guid cashAccountId, _) = await SeedCashPostingAsync(tenantId, 1_000m, asOf, fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteAccountBalanceLine> lines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [cashAccountId], null, asOf, null, CancellationToken.None);

        lines.Should().ContainSingle(l => l.TotalDebit == 1_000m);
    }

    [Fact]
    public async Task A_Period_Schedule_Excludes_Activity_Before_StartDate_And_Includes_EndDate()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly startDate = new(2026, 6, 1);
        DateOnly endDate = new(2026, 6, 30);

        (Guid cashAccountId, _) = await SeedCashPostingAsync(tenantId, 400m, new DateOnly(2026, 5, 31), fundId: null);
        await SeedCashPostingAsync(tenantId, 700m, endDate, fundId: null, existingCashAccountId: cashAccountId);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteAccountBalanceLine> lines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [cashAccountId], startDate, endDate, null, CancellationToken.None);

        lines.Should().ContainSingle();
        lines[0].TotalDebit.Should().Be(700m, "only activity inside [StartDate, EndDate] is period activity");
    }

    [Fact]
    public async Task A_Fund_Scoped_Schedule_Excludes_Another_Funds_Activity()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        Fund reserve = Fund.Create(tenantId, "RESERVE", "Reserve Fund", null);
        Fund general = Fund.Create(tenantId, "GENERAL", "General Fund", null);
        seedDb.Set<Fund>().AddRange(reserve, general);

        ChartOfAccount cash = CashAccount(tenantId);
        ChartOfAccount income = IncomeAccount(tenantId);
        seedDb.Set<ChartOfAccount>().AddRange(cash, income);

        AddPosting(seedDb, tenantId, asOf, 1_000m, cash, income, reserve.Id, "Test", null);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteAccountBalanceLine> reserveLines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [cash.Id.Value], null, asOf, reserve.Id.Value, CancellationToken.None);
        IReadOnlyList<NoteAccountBalanceLine> generalLines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [cash.Id.Value], null, asOf, general.Id.Value, CancellationToken.None);

        reserveLines.Should().ContainSingle(l => l.TotalDebit == 1_000m);
        generalLines.Should().BeEmpty(
            "a fund-scoped statement must never be explained by an organization-wide schedule");
    }

    [Fact]
    public async Task Account_Balances_With_No_Requested_Accounts_Skip_The_Query_Entirely()
    {
        Guid tenantId = Guid.NewGuid();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteAccountBalanceLine> lines = await repository.GetNoteAccountBalancesAsync(
            tenantId, [], null, new DateOnly(2026, 6, 30), null, CancellationToken.None);

        lines.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------------------------
    // Per-flat decomposition (Receivables / Resident Advances)
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Flat_Balances_Group_Receivable_Activity_By_Flat_And_Net_Allocations()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        Building building = Building.Create(tenantId, "Tower A", "TWR-A", null, Now);
        Flat flatOne = Flat.Create(tenantId, building.Id, "A-101", 1, FlatType.Residential, Now);
        Flat flatTwo = Flat.Create(tenantId, building.Id, "A-102", 1, FlatType.Residential, Now);
        seedDb.Set<Building>().Add(building);
        seedDb.Set<Flat>().AddRange(flatOne, flatTwo);

        ChartOfAccount receivable = ChartOfAccount.Create(
            tenantId, "1200", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.Receivables);
        ChartOfAccount income = IncomeAccount(tenantId);
        ChartOfAccount cash = CashAccount(tenantId);
        seedDb.Set<ChartOfAccount>().AddRange(receivable, income, cash);

        // Flat 1 billed 30,000 then a payment allocation settles 12,000 → 18,000 outstanding.
        AddFlatPosting(seedDb, tenantId, asOf, 30_000m, receivable, flatOne.Id, income, "InvoiceIssued", null);
        AddFlatPosting(seedDb, tenantId, asOf, 12_000m, cash, null, receivable, "PaymentAllocation", flatOne.Id);
        // Flat 2 billed 20,000 and fully settled → drops out of the schedule.
        AddFlatPosting(seedDb, tenantId, asOf, 20_000m, receivable, flatTwo.Id, income, "InvoiceIssued", null);
        AddFlatPosting(seedDb, tenantId, asOf, 20_000m, cash, null, receivable, "PaymentAllocation", flatTwo.Id);

        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteFlatBalanceLine> lines = await repository.GetNoteFlatBalancesAsync(
            tenantId, [receivable.Id.Value], null, asOf, null, CancellationToken.None);

        lines.Should().HaveCount(2);

        NoteFlatBalanceLine one = lines.Single(l => l.FlatId == flatOne.Id);
        (one.TotalDebit - one.TotalCredit).Should().Be(18_000m);

        NoteFlatBalanceLine two = lines.Single(l => l.FlatId == flatTwo.Id);
        (two.TotalDebit - two.TotalCredit).Should().Be(0m);
    }

    [Fact]
    public async Task Flat_Balances_With_No_Tenant_Context_Return_Nothing()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        Building building = Building.Create(tenantId, "Tower A", "TWR-A", null, Now);
        Flat flat = Flat.Create(tenantId, building.Id, "A-101", 1, FlatType.Residential, Now);
        seedDb.Set<Building>().Add(building);
        seedDb.Set<Flat>().Add(flat);

        ChartOfAccount receivable = ChartOfAccount.Create(
            tenantId, "1200", "Resident Receivable", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.Receivables);
        ChartOfAccount income = IncomeAccount(tenantId);
        seedDb.Set<ChartOfAccount>().AddRange(receivable, income);

        AddFlatPosting(seedDb, tenantId, asOf, 30_000m, receivable, flat.Id, income, "InvoiceIssued", null);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null);
        FinancialStatementNoteRepository repository = new(noTenant);

        IReadOnlyList<NoteFlatBalanceLine> lines = await repository.GetNoteFlatBalancesAsync(
            tenantId, [receivable.Id.Value], null, asOf, null, CancellationToken.None);

        lines.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------------------------
    // Per-posting-reference decomposition (Fixed Deposits, Payables, Expenses, Interest)
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Posting_Reference_Balances_Group_By_Source_Reference()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);
        Guid firstDepositId = Guid.NewGuid();
        Guid secondDepositId = Guid.NewGuid();

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        ChartOfAccount fixedDeposit = ChartOfAccount.Create(
            tenantId, "1100", "Fixed Deposits", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.InvestmentsAndFixedDeposits);
        ChartOfAccount cash = CashAccount(tenantId);
        seedDb.Set<ChartOfAccount>().AddRange(fixedDeposit, cash);

        AddPosting(seedDb, tenantId, asOf, 600_000m, fixedDeposit, cash, null, "FixedDepositPlacement", firstDepositId);
        AddPosting(seedDb, tenantId, asOf, 400_000m, fixedDeposit, cash, null, "FixedDepositPlacement", secondDepositId);
        AddPosting(seedDb, tenantId, asOf, 400_000m, cash, fixedDeposit, null, "FixedDepositMaturity", secondDepositId);
        // Renewal capitalization posts with no source id — the untraceable case the schedule must surface.
        AddPosting(seedDb, tenantId, asOf, 50_000m, fixedDeposit, cash, null, "FixedDepositRenewalCapitalization", null);

        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NotePostingReferenceLine> lines = await repository.GetNotePostingReferenceBalancesAsync(
            tenantId, [fixedDeposit.Id.Value], null, asOf, null, CancellationToken.None);

        lines.Should().Contain(l =>
            l.ReferenceType == "FixedDepositPlacement" && l.ReferenceId == firstDepositId && l.TotalDebit == 600_000m);
        lines.Should().Contain(l =>
            l.ReferenceType == "FixedDepositMaturity" && l.ReferenceId == secondDepositId && l.TotalCredit == 400_000m);
        lines.Should().Contain(l =>
            l.ReferenceType == "FixedDepositRenewalCapitalization" && l.ReferenceId == null && l.TotalDebit == 50_000m);
    }

    [Fact]
    public async Task Posting_Reference_Balances_Do_Not_Leak_Across_Tenants()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        (Guid cashA, _) = await SeedCashPostingAsync(tenantA, 1_000m, asOf, fundId: null);
        (Guid cashB, _) = await SeedCashPostingAsync(tenantB, 500m, asOf, fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantA);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NotePostingReferenceLine> lines = await repository.GetNotePostingReferenceBalancesAsync(
            tenantA, [cashA, cashB], null, asOf, null, CancellationToken.None);

        lines.Sum(l => l.TotalDebit).Should().Be(1_000m);
    }

    [Fact]
    public async Task Posting_References_Resolve_A_Reversals_Original_Business_Record()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);
        Guid expenseId = Guid.NewGuid();

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        ChartOfAccount operating = OperatingExpenseAccount(tenantId);
        ChartOfAccount payable = PayableAccount(tenantId);
        seedDb.Set<ChartOfAccount>().AddRange(operating, payable);

        LedgerPosting original = AddPosting(
            seedDb, tenantId, asOf, 25_000m, operating, payable, null, "ExpenseRecording", expenseId);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NotePostingReference> references = await repository.GetPostingReferencesAsync(
            tenantId, [original.Id.Value], CancellationToken.None);

        references.Should().ContainSingle();
        references[0].ReferenceType.Should().Be("ExpenseRecording");
        references[0].ReferenceId.Should().Be(expenseId,
            "a void posting references the original posting id, so this hop is what recovers the expense");
    }

    [Fact]
    public async Task Posting_References_Do_Not_Leak_Across_Tenants()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        (_, LedgerPosting postingB) = await SeedCashPostingAsync(tenantB, 500m, asOf, fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantA);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NotePostingReference> references = await repository.GetPostingReferencesAsync(
            tenantA, [postingB.Id.Value], CancellationToken.None);

        references.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------------------------
    // Drill-down lookups
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Expense_Refs_Resolve_Their_Category_Through_The_Expense_Type()
    {
        Guid tenantId = Guid.NewGuid();

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        ExpenseCategory category = ExpenseCategory.Create(tenantId, "Maintenance", "MAINT", null, 1, Now);
        ExpenseType type = ExpenseType.Create(tenantId, category.Id, "Lift AMC", "LIFT", null, 1, Now);
        Expense expense = Expense.Record(
            tenantId, null, type.Id, null, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 10),
            "Lift annual maintenance", "Acme Lifts Ltd", "INV-77", 60_000m, false, PaymentMethod.BankTransfer,
            null, Now);

        seedDb.Set<ExpenseCategory>().Add(category);
        seedDb.Set<ExpenseType>().Add(type);
        seedDb.Set<Expense>().Add(expense);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteExpenseRef> refs = await repository.GetExpenseRefsAsync(
            tenantId, [expense.Id.Value], CancellationToken.None);

        refs.Should().ContainSingle();
        refs[0].ExpenseCategoryId.Should().Be(category.Id);
        refs[0].ExpenseCategoryName.Should().Be("Maintenance");
        refs[0].ExpenseTypeName.Should().Be("Lift AMC");
        refs[0].Payee.Should().Be("Acme Lifts Ltd");
        refs[0].Amount.Should().Be(60_000m);
    }

    [Fact]
    public async Task Expense_Refs_Do_Not_Leak_Across_Tenants()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantB);

        ExpenseCategory category = ExpenseCategory.Create(tenantB, "Maintenance", "MAINT", null, 1, Now);
        ExpenseType type = ExpenseType.Create(tenantB, category.Id, "Lift AMC", "LIFT", null, 1, Now);
        Expense expense = Expense.Record(
            tenantB, null, type.Id, null, new DateOnly(2026, 6, 10), null, "Tenant B expense", null, null,
            10_000m, false, PaymentMethod.Cash, null, Now);

        seedDb.Set<ExpenseCategory>().Add(category);
        seedDb.Set<ExpenseType>().Add(type);
        seedDb.Set<Expense>().Add(expense);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantA);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteExpenseRef> refs = await repository.GetExpenseRefsAsync(
            tenantA, [expense.Id.Value], CancellationToken.None);

        refs.Should().BeEmpty();
    }

    [Fact]
    public async Task Flat_Refs_Resolve_The_Flat_Number_And_Building_Name()
    {
        Guid tenantId = Guid.NewGuid();

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        Building building = Building.Create(tenantId, "Tower A", "TWR-A", null, Now);
        Flat flat = Flat.Create(tenantId, building.Id, "A-101", 1, FlatType.Residential, Now);
        seedDb.Set<Building>().Add(building);
        seedDb.Set<Flat>().Add(flat);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteFlatRef> refs =
            await repository.GetFlatRefsAsync(tenantId, [flat.Id.Value], CancellationToken.None);

        refs.Should().ContainSingle();
        refs[0].FlatNumber.Should().Be("A-101");
        refs[0].BuildingName.Should().Be("Tower A");
    }

    [Fact]
    public async Task Financial_Account_Refs_Return_Only_The_Calling_Tenants_Accounts()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await SeedFinancialAccountAsync(tenantA, "Operating Account", "1234567890123");
        await SeedFinancialAccountAsync(tenantB, "Other Tenant Account", "9999999999999");

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantA);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteFinancialAccountRef> refs =
            await repository.GetFinancialAccountRefsAsync(tenantA, CancellationToken.None);

        refs.Should().ContainSingle();
        refs[0].Name.Should().Be("Operating Account");
        refs[0].AccountNumber.Should().Be("1234567890123",
            "the repository returns the raw value; masking is the Application layer's decision");
    }

    [Fact]
    public async Task Fixed_Deposit_And_Interest_Source_Refs_Resolve_Accruals_And_Receipts_To_Their_Deposit()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);

        ChartOfAccount cashAccount = CashAccount(tenantId);
        seedDb.Set<ChartOfAccount>().Add(cashAccount);

        FinancialAccount financialAccount = FinancialAccount.Create(
            tenantId, "Operating Account", FinancialAccountType.Bank, "City Bank", "Gulshan", "1234567890123",
            cashAccount.Id, null, null);
        seedDb.Set<FinancialAccount>().Add(financialAccount);

        FixedDepositId depositId = FixedDepositId.New();
        LedgerPostingId placementPostingId = LedgerPostingId.New();
        FixedDeposit deposit = FixedDeposit.Place(
            depositId, tenantId, "FD-001", "City Bank", "Gulshan", financialAccount.Id, null, 600_000m, 9.5m,
            InterestCalculationMethod.Simple, InterestPaymentFrequency.AtMaturity,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null, null, null, placementPostingId, Now);
        seedDb.Set<FixedDeposit>().Add(deposit);

        FixedDepositInterestAccrualId accrualId = FixedDepositInterestAccrualId.New();
        FixedDepositInterestAccrual accrual = FixedDepositInterestAccrual.Record(
            accrualId, tenantId, depositId, new DateOnly(2026, 6, 1), asOf, asOf, 15_000m, null,
            LedgerPostingId.New(), Now);
        seedDb.Set<FixedDepositInterestAccrual>().Add(accrual);

        FixedDepositInterestReceiptId receiptId = FixedDepositInterestReceiptId.New();
        FixedDepositInterestReceipt receipt = FixedDepositInterestReceipt.Record(
            receiptId, tenantId, depositId, asOf, 20_000m, 2_000m, financialAccount.Id, "REF-1", null,
            LedgerPostingId.New(), Now);
        seedDb.Set<FixedDepositInterestReceipt>().Add(receipt);

        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<NoteFixedDepositRef> depositRefs =
            await repository.GetFixedDepositRefsAsync(tenantId, [depositId.Value], CancellationToken.None);
        depositRefs.Should().ContainSingle();
        depositRefs[0].CertificateNumber.Should().Be("FD-001");
        depositRefs[0].Principal.Should().Be(600_000m);
        depositRefs[0].Status.Should().Be(FixedDepositStatus.Active);

        IReadOnlyList<NoteFixedDepositInterestSourceRef> sourceRefs =
            await repository.GetFixedDepositInterestSourceRefsAsync(
                tenantId, [accrualId.Value, receiptId.Value], CancellationToken.None);

        sourceRefs.Should().HaveCount(2);
        sourceRefs.Should().ContainSingle(r =>
            r.SourceId == accrualId.Value && !r.IsReceipt && r.FixedDepositId == depositId && r.GrossAmount == 15_000m);
        sourceRefs.Should().ContainSingle(r =>
            r.SourceId == receiptId.Value && r.IsReceipt && r.GrossAmount == 20_000m && r.DeductionAmount == 2_000m);
    }

    [Fact]
    public async Task Chart_Of_Accounts_Lookup_Is_Tenant_Scoped()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await SeedCashPostingAsync(tenantA, 1_000m, new DateOnly(2026, 6, 30), fundId: null);
        await SeedCashPostingAsync(tenantB, 500m, new DateOnly(2026, 6, 30), fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantA);
        FinancialStatementNoteRepository repository = new(db);

        IReadOnlyList<ChartOfAccount> accounts =
            await repository.GetChartOfAccountsAsync(tenantA, CancellationToken.None);

        accounts.Should().NotBeEmpty();
        accounts.Should().OnlyContain(a => a.TenantId == tenantA);
        accounts.Should().Contain(a => a.EffectiveStatementGroup == FinancialStatementGroup.CashAndBank);
    }

    // -------------------------------------------------------------------------------------------
    // Seeding helpers
    // -------------------------------------------------------------------------------------------

    private static ChartOfAccount CashAccount(Guid tenantId) => ChartOfAccount.Create(
        tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
        statementGroup: FinancialStatementGroup.CashAndBank);

    private static ChartOfAccount IncomeAccount(Guid tenantId) => ChartOfAccount.Create(
        tenantId, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
        statementGroup: FinancialStatementGroup.ServiceChargeIncome);

    private static ChartOfAccount OperatingExpenseAccount(Guid tenantId) => ChartOfAccount.Create(
        tenantId, "5000", "Operating Expenses", AccountCategory.Expense, LedgerDirection.Debit,
        statementGroup: FinancialStatementGroup.OperatingExpenses);

    private static ChartOfAccount PayableAccount(Guid tenantId) => ChartOfAccount.Create(
        tenantId, "2000", "Accounts Payable", AccountCategory.Liability, LedgerDirection.Credit,
        statementGroup: FinancialStatementGroup.AccountsPayable);

    private async Task<(Guid CashAccountId, LedgerPosting Posting)> SeedCashPostingAsync(
        Guid tenantId, decimal amount, DateOnly businessDate, Guid? fundId, Guid? existingCashAccountId = null)
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        ChartOfAccount cash;
        if (existingCashAccountId is Guid existing)
        {
            cash = await db.Set<ChartOfAccount>().FindAsync(new ChartOfAccountId(existing))
                ?? throw new InvalidOperationException("Seeded cash account not found.");
        }
        else
        {
            cash = CashAccount(tenantId);
            db.Set<ChartOfAccount>().Add(cash);
        }

        ChartOfAccount income = ChartOfAccount.Create(
            tenantId, $"4{Random.Shared.Next(100, 999)}", "Association Revenue", AccountCategory.Income,
            LedgerDirection.Credit, statementGroup: FinancialStatementGroup.ServiceChargeIncome);
        db.Set<ChartOfAccount>().Add(income);

        FundId? typedFundId = fundId is Guid f ? new FundId(f) : null;
        LedgerPosting posting = AddPosting(db, tenantId, businessDate, amount, cash, income, typedFundId, "Test", null);

        await db.SaveChangesAsync();
        return (cash.Id.Value, posting);
    }

    private async Task SeedFinancialAccountAsync(Guid tenantId, string name, string accountNumber)
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        ChartOfAccount cash = CashAccount(tenantId);
        db.Set<ChartOfAccount>().Add(cash);

        db.Set<FinancialAccount>().Add(FinancialAccount.Create(
            tenantId, name, FinancialAccountType.Bank, "City Bank", "Gulshan", accountNumber, cash.Id, null, null));

        await db.SaveChangesAsync();
    }

    /// <summary>Adds a two-line balanced posting (debit <paramref name="debitAccount"/>, credit
    /// <paramref name="creditAccount"/>) with the given source reference — neither line is flat-scoped.</summary>
    private static LedgerPosting AddPosting(
        MyCondoDbContext db, Guid tenantId, DateOnly businessDate, decimal amount,
        ChartOfAccount debitAccount, ChartOfAccount creditAccount, FundId? fundId,
        string referenceType, Guid? referenceId)
    {
        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, businessDate, "Note schedule test posting", referenceType, referenceId,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount, "Debit leg"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, amount, "Credit leg"),
            ],
            Now);

        entries[0].SetFinanceDimensions(debitAccount.Id, fundId, null);
        entries[1].SetFinanceDimensions(creditAccount.Id, fundId, null);

        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);
        return posting;
    }

    /// <summary>Adds a posting where exactly one leg is flat-scoped, mirroring how the posting engine
    /// stamps <c>FlatId</c> onto <c>ResidentReceivable</c>/<c>ResidentAdvance</c> lines. Pass
    /// <paramref name="debitFlatId"/> for a charge (debit side flat-scoped) or
    /// <paramref name="creditFlatId"/> for a settlement (credit side flat-scoped).</summary>
    private static void AddFlatPosting(
        MyCondoDbContext db, Guid tenantId, DateOnly businessDate, decimal amount,
        ChartOfAccount debitAccount, FlatId? debitFlatId, ChartOfAccount creditAccount,
        string referenceType, FlatId? creditFlatId)
    {
        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, businessDate, "Note schedule flat posting", referenceType, Guid.NewGuid(),
            [
                debitFlatId is null
                    ? new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount, "Debit leg")
                    : new LedgerLine(LedgerAccountType.ResidentReceivable, debitFlatId, LedgerDirection.Debit, amount, "Charge"),
                creditFlatId is null
                    ? new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, amount, "Credit leg")
                    : new LedgerLine(LedgerAccountType.ResidentReceivable, creditFlatId, LedgerDirection.Credit, amount, "Settlement"),
            ],
            Now);

        entries[0].SetFinanceDimensions(debitAccount.Id, null, null);
        entries[1].SetFinanceDimensions(creditAccount.Id, null, null);

        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);
    }
}
