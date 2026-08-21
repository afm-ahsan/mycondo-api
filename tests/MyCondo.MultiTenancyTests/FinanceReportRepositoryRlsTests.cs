using AwesomeAssertions;
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
