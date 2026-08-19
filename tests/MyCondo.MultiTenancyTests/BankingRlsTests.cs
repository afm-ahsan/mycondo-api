using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the new finance.financial_accounts/fixed_deposits/
/// fixed_deposit_interest_accruals/fixed_deposit_interest_receipts tables (Template 4 — Banking, Fixed
/// Deposits &amp; Interest), same pattern as FinanceRlsTests/ExpensesRlsTests. Requires a Docker daemon;
/// not executed in the environment this was authored in — see MultiTenancyPostgresFixture's doc comment.
/// </summary>
public class BankingRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public BankingRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FinancialAccounts_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<FinancialAccount>().Add(FinancialAccount.Create(
                tenantA, "Tenant A Bank", FinancialAccountType.Bank, "City Bank", null, null,
                ChartOfAccountId.New(), null, null));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<FinancialAccount>().Add(FinancialAccount.Create(
                tenantB, "Tenant B Bank", FinancialAccountType.Bank, "Other Bank", null, null,
                ChartOfAccountId.New(), null, null));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<FinancialAccount> visible = await asTenantA.Set<FinancialAccount>().ToListAsync();
            visible.Should().ContainSingle(a => a.Name == "Tenant A Bank");
            visible.Should().NotContain(a => a.Name == "Tenant B Bank");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<FinancialAccount> visible = await noTenant.Set<FinancialAccount>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's financial accounts");
        }
    }

    [Fact]
    public async Task FixedDeposits_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<FixedDeposit>().Add(PlaceFixedDeposit(tenantA, "FD-A-001"));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<FixedDeposit>().Add(PlaceFixedDeposit(tenantB, "FD-B-001"));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<FixedDeposit> visible = await asTenantA.Set<FixedDeposit>().ToListAsync();
            visible.Should().ContainSingle(f => f.CertificateNumber == "FD-A-001");
            visible.Should().NotContain(f => f.CertificateNumber == "FD-B-001");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<FixedDeposit> visible = await noTenant.Set<FixedDeposit>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's fixed deposits");
        }
    }

    [Fact]
    public async Task FixedDepositInterestAccruals_And_Receipts_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        FixedDepositId fixedDepositIdA = FixedDepositId.New();
        FixedDepositId fixedDepositIdB = FixedDepositId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<FixedDepositInterestAccrual>().Add(FixedDepositInterestAccrual.Record(
                FixedDepositInterestAccrualId.New(), tenantA, fixedDepositIdA, new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 31), 3_000m, null, LedgerPostingId.New(),
                DateTimeOffset.UtcNow));
            dbA.Set<FixedDepositInterestReceipt>().Add(FixedDepositInterestReceipt.Record(
                FixedDepositInterestReceiptId.New(), tenantA, fixedDepositIdA, new DateOnly(2026, 1, 31),
                3_000m, 0m, FinancialAccountId.New(), null, null, LedgerPostingId.New(), DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<FixedDepositInterestAccrual>().Add(FixedDepositInterestAccrual.Record(
                FixedDepositInterestAccrualId.New(), tenantB, fixedDepositIdB, new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 31), 5_000m, null, LedgerPostingId.New(),
                DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<FixedDepositInterestAccrual> visibleAccruals = await asTenantA.Set<FixedDepositInterestAccrual>().ToListAsync();
            visibleAccruals.Should().ContainSingle(a => a.GrossAmount == 3_000m);
            visibleAccruals.Should().NotContain(a => a.GrossAmount == 5_000m);

            List<FixedDepositInterestReceipt> visibleReceipts = await asTenantA.Set<FixedDepositInterestReceipt>().ToListAsync();
            visibleReceipts.Should().ContainSingle(r => r.GrossAmount == 3_000m);
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<FixedDepositInterestAccrual> visible = await noTenant.Set<FixedDepositInterestAccrual>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's interest accruals");
        }
    }

    [Fact]
    public async Task Insert_FixedDeposit_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it.
        dbAsTenantB.Set<FixedDeposit>().Add(PlaceFixedDeposit(tenantA, "FD-WRONG-TENANT"));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Theory]
    [InlineData("finance.financial_accounts")]
    [InlineData("finance.fixed_deposits")]
    [InlineData("finance.fixed_deposit_interest_accruals")]
    [InlineData("finance.fixed_deposit_interest_receipts")]
    public async Task Banking_Tables_Have_Rls_Enabled_And_Forced(string qualifiedTableName)
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

    private static FixedDeposit PlaceFixedDeposit(Guid tenantId, string certificateNumber) =>
        FixedDeposit.Place(
            FixedDepositId.New(), tenantId, certificateNumber, "City Bank", null, FinancialAccountId.New(), null,
            500_000m, 7.5m, InterestCalculationMethod.Simple, InterestPaymentFrequency.Monthly,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null, null, null, LedgerPostingId.New(),
            DateTimeOffset.UtcNow);

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
