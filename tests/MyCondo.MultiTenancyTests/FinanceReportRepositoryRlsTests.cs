using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Template 5's <see cref="FinanceReportRepository"/> is a new read path over the existing RLS-
/// protected `finance`/`payments` schema (Templates 1-4) — it introduces no new tables, so
/// <see cref="FinanceRlsTests"/>'s "RLS enabled and forced" theory already covers the underlying
/// tables. This suite instead proves the new *queries themselves* (GroupBy/join aggregation across
/// LedgerEntry/LedgerPosting/ChartOfAccount) don't accidentally leak cross-tenant rows into a Trial
/// Balance or General Ledger — all EF LINQ, no raw SQL, so RLS applies automatically via the
/// connection-level `app.current_tenant_id`, but that's exactly the assumption worth verifying against
/// real Postgres rather than trusting by inspection. Requires a Docker daemon — see
/// MultiTenancyPostgresFixture's doc comment.
/// </summary>
public class FinanceReportRepositoryRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public FinanceReportRepositoryRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTrialBalanceAsync_Only_Returns_The_Calling_Tenants_Activity()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        await SeedBalancedPostingAsync(tenantA, "1000", "Cash & Bank", "4000", "Association Revenue", 1_000m, today);
        await SeedBalancedPostingAsync(tenantB, "1000", "Cash & Bank", "4000", "Association Revenue", 500m, today);

        await using MyCondoDbContext dbAsTenantA = _fixture.CreateDbContext(tenantA);
        FinanceReportRepository repository = new(dbAsTenantA);

        IReadOnlyList<TrialBalanceAccountLine> lines = await repository.GetTrialBalanceAsync(tenantA, today, CancellationToken.None);

        lines.Should().OnlyContain(l => l.TotalDebit == 1_000m || l.TotalCredit == 1_000m,
            "tenant B's 500-unit posting must never appear in tenant A's Trial Balance");
    }

    [Fact]
    public async Task GetGeneralLedgerAsync_Only_Returns_The_Calling_Tenants_Entries()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        await SeedBalancedPostingAsync(tenantA, "1000", "Cash & Bank", "4000", "Association Revenue", 1_000m, today);
        await SeedBalancedPostingAsync(tenantB, "1000", "Cash & Bank", "4000", "Association Revenue", 500m, today);

        await using MyCondoDbContext dbAsTenantA = _fixture.CreateDbContext(tenantA);
        FinanceReportRepository repository = new(dbAsTenantA);

        Domain.Common.PagedResult<LedgerActivityLine> page = await repository.GetGeneralLedgerAsync(
            tenantA, fromDate: null, toDate: null, chartOfAccountId: null, fundId: null, referenceType: null,
            page: 1, pageSize: 50, ascending: true, CancellationToken.None);

        page.Items.Should().OnlyContain(l => l.Amount == 1_000m, "tenant B's entries must never appear in tenant A's General Ledger");
        page.Total.Should().Be(2); // the debit + credit line of tenant A's one posting
    }

    [Fact]
    public async Task GetTrialBalanceAsync_With_No_Tenant_Context_Returns_Nothing()
    {
        Guid tenantA = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        await SeedBalancedPostingAsync(tenantA, "1000", "Cash & Bank", "4000", "Association Revenue", 1_000m, today);

        await using MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null);
        FinanceReportRepository repository = new(noTenant);

        IReadOnlyList<TrialBalanceAccountLine> lines = await repository.GetTrialBalanceAsync(tenantA, today, CancellationToken.None);

        lines.Should().BeEmpty("a connection with no tenant context set must default-deny, not see any tenant's ledger");
    }

    /// <summary>
    /// Regression test for the Cash Flow report's 500 error: <c>GetCashMovementsAsync</c> used to filter
    /// entries by chart-of-account membership with <c>List&lt;T&gt;.Contains(e.ChartOfAccountId.Value.Value)</c>
    /// inside the SQL predicate, which throws <c>System.InvalidOperationException</c> ("could not be
    /// translated") under the current EF Core/Npgsql provider for every collection shape tried — only
    /// caught by exercising the query against real Postgres, not a mocked repository.
    /// </summary>
    [Fact]
    public async Task GetCashMovementsAsync_Succeeds_Against_Real_Postgres_And_Classifies_Operating_Activity()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using MyCondoDbContext seedDb = _fixture.CreateDbContext(tenantId);
        ChartOfAccount cashAccount = ChartOfAccount.Create(tenantId, "1000", "Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit);
        ChartOfAccount revenueAccount = ChartOfAccount.Create(tenantId, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit);
        seedDb.Set<ChartOfAccount>().AddRange(cashAccount, revenueAccount);
        seedDb.Set<AccountMapping>().Add(AccountMapping.Create(tenantId, nameof(LedgerAccountType.CashOrBank), cashAccount.Id));

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, today, "Cash flow regression test posting", "Test", null,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 400m, "Cash in"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 400m, "Revenue"),
            ],
            DateTimeOffset.UtcNow);
        entries[0].SetFinanceDimensions(cashAccount.Id, null, null);
        entries[1].SetFinanceDimensions(revenueAccount.Id, null, null);
        seedDb.Set<LedgerPosting>().Add(posting);
        seedDb.Set<LedgerEntry>().AddRange(entries);
        await seedDb.SaveChangesAsync();

        await using MyCondoDbContext dbAsTenant = _fixture.CreateDbContext(tenantId);
        FinanceReportRepository repository = new(dbAsTenant);

        IReadOnlyList<CashMovementLine> movements =
            await repository.GetCashMovementsAsync(tenantId, today, today, CancellationToken.None);

        movements.Should().ContainSingle();
        movements[0].Amount.Should().Be(400m);
        movements[0].Direction.Should().Be(LedgerDirection.Debit);
        movements[0].IsInvestingCounterLeg.Should().BeFalse("Association Revenue is Operating, not Investing");
    }

    /// <summary>
    /// Regression test for the Expense Trend report's 500 error: <c>GetAccountMonthlyActivityAsync</c>
    /// used to project straight into the <c>MonthlyAccountActivityLine</c> record inside a
    /// <c>GroupBy</c>/<c>Select</c>, which throws <c>System.InvalidOperationException</c> ("could not be
    /// translated") under the current EF Core/Npgsql provider — only caught against real Postgres.
    /// </summary>
    [Fact]
    public async Task GetAccountMonthlyActivityAsync_Succeeds_Against_Real_Postgres_For_The_Calling_Tenant()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Reuses the debit/credit posting helper purely to get a posted LedgerEntry on a known
        // ChartOfAccountId — GetAccountMonthlyActivityAsync only filters by ChartOfAccountId, not by
        // AccountCategory or LedgerAccountType, so the "Operating Expense"-named account being seeded
        // under AccountCategory.Asset (the helper's fixed debit-side category) doesn't affect this test.
        await SeedBalancedPostingAsync(tenantId, "5000", "Operating Expense", "1000", "Cash & Bank", 250m, today);

        await using MyCondoDbContext lookupDb = _fixture.CreateDbContext(tenantId);
        ChartOfAccount expenseAccount = await lookupDb.Set<ChartOfAccount>()
            .SingleAsync(a => a.TenantId == tenantId && a.Code == "5000");

        await using MyCondoDbContext dbAsTenant = _fixture.CreateDbContext(tenantId);
        FinanceReportRepository repository = new(dbAsTenant);

        IReadOnlyList<MonthlyAccountActivityLine> monthly = await repository.GetAccountMonthlyActivityAsync(
            tenantId, expenseAccount.Id.Value, today, today, CancellationToken.None);

        monthly.Should().ContainSingle();
        monthly[0].Year.Should().Be(today.Year);
        monthly[0].Month.Should().Be(today.Month);
        monthly[0].TotalDebit.Should().Be(250m);
        monthly[0].TotalCredit.Should().Be(0m);
    }

    private async Task SeedBalancedPostingAsync(
        Guid tenantId, string debitCode, string debitName, string creditCode, string creditName, decimal amount, DateOnly businessDate)
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        ChartOfAccount debitAccount = ChartOfAccount.Create(tenantId, debitCode, debitName, AccountCategory.Asset, LedgerDirection.Debit);
        ChartOfAccount creditAccount = ChartOfAccount.Create(tenantId, creditCode, creditName, AccountCategory.Income, LedgerDirection.Credit);
        db.Set<ChartOfAccount>().AddRange(debitAccount, creditAccount);

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, businessDate, "RLS test posting", "Test", null,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount, "Cash in"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, amount, "Revenue"),
            ],
            DateTimeOffset.UtcNow);

        entries[0].SetFinanceDimensions(debitAccount.Id, null, null);
        entries[1].SetFinanceDimensions(creditAccount.Id, null, null);

        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);

        await db.SaveChangesAsync();
    }
}
