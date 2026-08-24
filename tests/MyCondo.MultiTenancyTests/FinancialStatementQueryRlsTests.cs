using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// CondoBD Finance Phase 2A Task 2's <see cref="FinanceReportRepository.GetStatementBalancesAsOfAsync"/>
/// and <see cref="FinanceReportRepository.GetStatementActivityForPeriodAsync"/> are new EF LINQ queries
/// over the same RLS-protected `finance`/`payments` schema <see cref="FinanceReportRepositoryRlsTests"/>
/// already covers for the pre-existing report queries — this suite proves the same "no cross-tenant
/// leakage" property for the two new Financial Statements foundation queries, plus the date-boundary,
/// fund-scoping, and reversal-netting behavior specific to them, against real ledger postings. Requires
/// a Docker daemon — see <see cref="MultiTenancyPostgresFixture"/>'s doc comment.
/// </summary>
public class FinancialStatementQueryRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public FinancialStatementQueryRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetStatementBalancesAsOfAsync_Only_Returns_The_Calling_Tenants_Activity()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await SeedPostingAsync(tenantA, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            FinancialStatementGroup.CashAndBank, FinancialStatementGroup.ServiceChargeIncome,
            1_000m, asOf, fundId: null);
        await SeedPostingAsync(tenantB, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            FinancialStatementGroup.CashAndBank, FinancialStatementGroup.ServiceChargeIncome,
            500m, asOf, fundId: null);

        await using MyCondoDbContext dbAsTenantA = _fixture.CreateDbContext(tenantA);
        FinanceReportRepository repository = new(dbAsTenantA);

        IReadOnlyList<StatementAccountActivityLine> lines =
            await repository.GetStatementBalancesAsOfAsync(tenantA, asOf, null, CancellationToken.None);

        lines.Should().OnlyContain(l => l.TotalDebit == 1_000m || l.TotalCredit == 1_000m,
            "tenant B's 500-unit posting must never appear in tenant A's Financial Statements foundation query");
    }

    [Fact]
    public async Task GetStatementBalancesAsOfAsync_With_No_Tenant_Context_Returns_Nothing()
    {
        Guid tenantA = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await SeedPostingAsync(tenantA, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            FinancialStatementGroup.CashAndBank, FinancialStatementGroup.ServiceChargeIncome,
            1_000m, asOf, fundId: null);

        await using MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null);
        FinanceReportRepository repository = new(noTenant);

        IReadOnlyList<StatementAccountActivityLine> lines =
            await repository.GetStatementBalancesAsOfAsync(tenantA, asOf, null, CancellationToken.None);

        lines.Should().BeEmpty("a connection with no tenant context set must default-deny, not see any tenant's ledger");
    }

    [Fact]
    public async Task GetStatementBalancesAsOfAsync_Excludes_Activity_After_The_AsOf_Date()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);
        DateOnly afterAsOf = new(2026, 7, 1);

        await SeedPostingAsync(tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            FinancialStatementGroup.CashAndBank, FinancialStatementGroup.ServiceChargeIncome,
            1_000m, afterAsOf, fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinanceReportRepository repository = new(db);

        IReadOnlyList<StatementAccountActivityLine> lines =
            await repository.GetStatementBalancesAsOfAsync(tenantId, asOf, null, CancellationToken.None);

        lines.Should().BeEmpty("a posting dated after the as-of date must not affect the Statement of Financial Position");
    }

    [Fact]
    public async Task GetStatementActivityForPeriodAsync_Excludes_Balances_Strictly_Before_StartDate()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly beforeStart = new(2026, 5, 31);
        DateOnly startDate = new(2026, 6, 1);
        DateOnly endDate = new(2026, 6, 30);

        await SeedPostingAsync(tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            FinancialStatementGroup.CashAndBank, FinancialStatementGroup.ServiceChargeIncome,
            1_000m, beforeStart, fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinanceReportRepository repository = new(db);

        IReadOnlyList<StatementAccountActivityLine> lines =
            await repository.GetStatementActivityForPeriodAsync(tenantId, startDate, endDate, null, CancellationToken.None);

        lines.Should().BeEmpty("an opening balance posted before StartDate is not current-period activity");
    }

    [Fact]
    public async Task GetStatementActivityForPeriodAsync_Includes_Activity_On_The_EndDate()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly startDate = new(2026, 6, 1);
        DateOnly endDate = new(2026, 6, 30);

        await SeedPostingAsync(tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            FinancialStatementGroup.CashAndBank, FinancialStatementGroup.ServiceChargeIncome,
            1_000m, endDate, fundId: null);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FinanceReportRepository repository = new(db);

        IReadOnlyList<StatementAccountActivityLine> lines =
            await repository.GetStatementActivityForPeriodAsync(tenantId, startDate, endDate, null, CancellationToken.None);

        lines.Should().Contain(l => l.Code == "4000" && l.TotalCredit == 1_000m,
            "a posting dated exactly on EndDate is inside the [StartDate, EndDate] inclusive window");
    }

    [Fact]
    public async Task A_Reversal_Posting_Nets_The_Original_Activity_To_Zero()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly startDate = new(2026, 6, 1);
        DateOnly endDate = new(2026, 6, 30);
        DateOnly postingDate = new(2026, 6, 10);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        ChartOfAccount cash = ChartOfAccount.Create(tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.CashAndBank);
        ChartOfAccount income = ChartOfAccount.Create(tenantId, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            statementGroup: FinancialStatementGroup.ServiceChargeIncome);
        db.Set<ChartOfAccount>().AddRange(cash, income);

        (LedgerPosting original, IReadOnlyList<LedgerEntry> originalEntries) = LedgerPosting.Create(
            tenantId, postingDate, "Original charge", "Test", null,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 1_000m, "Cash in"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 1_000m, "Revenue"),
            ],
            DateTimeOffset.UtcNow);
        originalEntries[0].SetFinanceDimensions(cash.Id, null, null);
        originalEntries[1].SetFinanceDimensions(income.Id, null, null);

        (LedgerPosting reversal, IReadOnlyList<LedgerEntry> reversalEntries) = LedgerPosting.Create(
            tenantId, postingDate, "Reversal of original charge", "Test-Reversal", null,
            [
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Debit, 1_000m, "Revenue reversed"),
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, 1_000m, "Cash out"),
            ],
            DateTimeOffset.UtcNow);
        reversalEntries[0].SetFinanceDimensions(income.Id, null, null);
        reversalEntries[1].SetFinanceDimensions(cash.Id, null, null);

        db.Set<LedgerPosting>().AddRange(original, reversal);
        db.Set<LedgerEntry>().AddRange(originalEntries.Concat(reversalEntries));
        await db.SaveChangesAsync();

        FinanceReportRepository repository = new(db);

        IReadOnlyList<StatementAccountActivityLine> lines =
            await repository.GetStatementActivityForPeriodAsync(tenantId, startDate, endDate, null, CancellationToken.None);

        lines.Should().OnlyContain(l => l.TotalDebit == l.TotalCredit,
            "the reversal's opposite entries must land in the same aggregation window and net to zero activity");
    }

    [Fact]
    public async Task FundId_Filter_Excludes_Activity_Tagged_To_A_Different_Fund()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly asOf = new(2026, 6, 30);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        Fund reserveFund = Fund.Create(tenantId, "RESERVE", "Reserve Fund", null);
        Fund generalFund = Fund.Create(tenantId, "GENERAL", "General Fund", null);
        db.Set<Fund>().AddRange(reserveFund, generalFund);

        ChartOfAccount cash = ChartOfAccount.Create(tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.CashAndBank);
        ChartOfAccount income = ChartOfAccount.Create(tenantId, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            statementGroup: FinancialStatementGroup.ServiceChargeIncome);
        db.Set<ChartOfAccount>().AddRange(cash, income);

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, asOf, "Reserve fund contribution", "Test", null,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 1_000m, "Cash in"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 1_000m, "Revenue"),
            ],
            DateTimeOffset.UtcNow);
        entries[0].SetFinanceDimensions(cash.Id, reserveFund.Id, null);
        entries[1].SetFinanceDimensions(income.Id, reserveFund.Id, null);

        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);
        await db.SaveChangesAsync();

        FinanceReportRepository repository = new(db);

        IReadOnlyList<StatementAccountActivityLine> generalFundLines =
            await repository.GetStatementBalancesAsOfAsync(tenantId, asOf, generalFund.Id.Value, CancellationToken.None);
        IReadOnlyList<StatementAccountActivityLine> reserveFundLines =
            await repository.GetStatementBalancesAsOfAsync(tenantId, asOf, reserveFund.Id.Value, CancellationToken.None);

        generalFundLines.Should().BeEmpty("the posting was tagged to the Reserve Fund, not the General Fund");
        reserveFundLines.Should().Contain(l => l.Code == "1000" && l.TotalDebit == 1_000m);
    }

    private async Task SeedPostingAsync(
        Guid tenantId,
        string debitCode, string debitName, AccountCategory debitCategory, LedgerDirection debitNormalBalance,
        string creditCode, string creditName, AccountCategory creditCategory, LedgerDirection creditNormalBalance,
        FinancialStatementGroup debitGroup, FinancialStatementGroup creditGroup,
        decimal amount, DateOnly businessDate, Guid? fundId)
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        ChartOfAccount debitAccount = ChartOfAccount.Create(
            tenantId, debitCode, debitName, debitCategory, debitNormalBalance, statementGroup: debitGroup);
        ChartOfAccount creditAccount = ChartOfAccount.Create(
            tenantId, creditCode, creditName, creditCategory, creditNormalBalance, statementGroup: creditGroup);
        db.Set<ChartOfAccount>().AddRange(debitAccount, creditAccount);

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, businessDate, "Financial Statements RLS test posting", "Test", null,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount, "Cash in"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, amount, "Revenue"),
            ],
            DateTimeOffset.UtcNow);

        FundId? typedFundId = fundId is Guid f ? new FundId(f) : null;
        entries[0].SetFinanceDimensions(debitAccount.Id, typedFundId, null);
        entries[1].SetFinanceDimensions(creditAccount.Id, typedFundId, null);

        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);

        await db.SaveChangesAsync();
    }
}
