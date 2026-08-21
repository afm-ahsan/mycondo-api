using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the new `finance` schema (ADR-027 — Finance Foundation & Posting
/// Engine), same pattern as ExpensesRlsTests. Requires a Docker daemon; not executed in the environment
/// this was authored in — see MultiTenancyPostgresFixture's doc comment.
/// </summary>
public class FinanceRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public FinanceRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ChartOfAccounts_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<ChartOfAccount>().Add(ChartOfAccount.Create(tenantA, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<ChartOfAccount>().Add(ChartOfAccount.Create(tenantB, "4000", "Association Revenue", AccountCategory.Income, LedgerDirection.Credit));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<ChartOfAccount> visible = await asTenantA.Set<ChartOfAccount>().ToListAsync();
            visible.Should().ContainSingle(a => a.Code == "1000");
            visible.Should().NotContain(a => a.Code == "4000");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<ChartOfAccount> visible = await noTenant.Set<ChartOfAccount>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's chart of accounts");
        }
    }

    [Fact]
    public async Task Funds_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Fund>().Add(Fund.Create(tenantA, "GEN", "Accumulated Association Fund", null));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Fund>().Add(Fund.Create(tenantB, "RES", "Reserve Fund", null));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Fund> visible = await asTenantA.Set<Fund>().ToListAsync();
            visible.Should().ContainSingle(f => f.Code == "GEN");
            visible.Should().NotContain(f => f.Code == "RES");
        }
    }

    [Fact]
    public async Task Insert_ChartOfAccount_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it.
        dbAsTenantB.Set<ChartOfAccount>().Add(
            ChartOfAccount.Create(tenantA, "1000", "Cash / Bank", AccountCategory.Asset, LedgerDirection.Debit));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task FinanceAuditLog_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<FinanceAuditLogEntry>().Add(
                FinanceAuditLogEntry.Record(tenantA, DateTimeOffset.UtcNow, null, "AccountingPeriod.Close"));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<FinanceAuditLogEntry>().Add(
                FinanceAuditLogEntry.Record(tenantB, DateTimeOffset.UtcNow, null, "AccountingPeriod.Reopen"));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<FinanceAuditLogEntry> visible = await asTenantA.Set<FinanceAuditLogEntry>().ToListAsync();
            visible.Should().ContainSingle(e => e.Action == "AccountingPeriod.Close");
            visible.Should().NotContain(e => e.Action == "AccountingPeriod.Reopen");
        }
    }

    [Fact]
    public async Task BankReconciliation_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        Domain.Features.Finance.FinancialAccounts.FinancialAccountId financialAccountId =
            Domain.Features.Finance.FinancialAccounts.FinancialAccountId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<BankReconciliation>().Add(
                BankReconciliation.Start(tenantA, financialAccountId, new DateOnly(2026, 8, 31), 1000m, 900m));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<BankReconciliation>().Add(
                BankReconciliation.Start(tenantB, financialAccountId, new DateOnly(2026, 8, 31), 2000m, 1900m));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<BankReconciliation> visible = await asTenantA.Set<BankReconciliation>().ToListAsync();
            visible.Should().ContainSingle(r => r.StatementBalance == 1000m);
            visible.Should().NotContain(r => r.StatementBalance == 2000m);
        }
    }

    [Fact]
    public async Task BankStatementLine_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        BankReconciliationId reconciliationId = BankReconciliationId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<BankStatementLine>().Add(
                BankStatementLine.Add(tenantA, reconciliationId, new DateOnly(2026, 8, 15), "Tenant A deposit", 500m));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<BankStatementLine>().Add(
                BankStatementLine.Add(tenantB, reconciliationId, new DateOnly(2026, 8, 15), "Tenant B deposit", 700m));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<BankStatementLine> visible = await asTenantA.Set<BankStatementLine>().ToListAsync();
            visible.Should().ContainSingle(l => l.Description == "Tenant A deposit");
            visible.Should().NotContain(l => l.Description == "Tenant B deposit");
        }
    }

    [Theory]
    [InlineData("finance.chart_of_accounts")]
    [InlineData("finance.account_mappings")]
    [InlineData("finance.funds")]
    [InlineData("finance.financial_years")]
    [InlineData("finance.accounting_periods")]
    [InlineData("finance.finance_audit_log")]
    [InlineData("finance.bank_reconciliations")]
    [InlineData("finance.bank_statement_lines")]
    public async Task Finance_Tables_Have_Rls_Enabled_And_Forced(string qualifiedTableName)
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        RlsFlags row = await db.Database
            .SqlQuery<RlsFlags>(
                $"""
                SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                FROM pg_class
                WHERE oid = {qualifiedTableName}::regclass
                """)
            .SingleAsync();

        row.RowSecurity.Should().BeTrue($"{qualifiedTableName} must have RLS enabled");
        row.ForceRowSecurity.Should().BeTrue($"{qualifiedTableName} must FORCE RLS (the app's DB role owns the table)");
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
