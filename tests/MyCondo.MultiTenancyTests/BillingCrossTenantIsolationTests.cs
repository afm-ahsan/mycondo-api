using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the Slice E `billing` schema tables (service_charge_rules,
/// invoices, invoice_lines, invoice_sequences) plus the invoice-sequence atomic-increment guarantee
/// — same pattern as <see cref="PaymentsCrossTenantIsolationTests"/>. Requires a Docker daemon.
/// Written and reviewed for correctness but NOT executed in the environment they were authored in —
/// run wherever Docker is actually available before trusting them.
/// </summary>
public class BillingCrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public BillingCrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ServiceChargeRules_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        BuildingId buildingA = BuildingId.New();
        BuildingId buildingB = BuildingId.New();
        DateOnly effectiveFrom = new(2026, 1, 1);

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<ServiceChargeRule>().Add(ServiceChargeRule.Create(
                tenantA, buildingA, "ServiceCharge", "Tenant A Charge", CalculationMethod.FixedAmount, 1500m, null,
                BillingFrequency.Monthly, effectiveFrom, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<ServiceChargeRule>().Add(ServiceChargeRule.Create(
                tenantB, buildingB, "ServiceCharge", "Tenant B Charge", CalculationMethod.FixedAmount, 2000m, null,
                BillingFrequency.Monthly, effectiveFrom, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<ServiceChargeRule> visible = await asTenantA.Set<ServiceChargeRule>().ToListAsync();
            visible.Should().ContainSingle(r => r.Name == "Tenant A Charge");
            visible.Should().NotContain(r => r.Name == "Tenant B Charge");
        }
    }

    [Fact]
    public async Task Insert_ServiceChargeRule_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        dbAsTenantB.Set<ServiceChargeRule>().Add(ServiceChargeRule.Create(
            tenantA, BuildingId.New(), "ServiceCharge", "Impersonator Charge", CalculationMethod.FixedAmount, 1500m,
            null, BillingFrequency.Monthly, new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Slice_E_Billing_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("billing", "service_charge_rules"),
            ("billing", "invoices"),
            ("billing", "invoice_lines"),
            ("billing", "invoice_sequences"),
            ("payments", "payment_allocations"),
        ];

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        foreach ((string schema, string table) in tables)
        {
            RlsFlags row = await db.Database
                .SqlQuery<RlsFlags>(
                    $"""
                    SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                    FROM pg_class
                    WHERE oid = ({schema + "."} || {table})::regclass
                    """)
                .SingleAsync();

            row.RowSecurity.Should().BeTrue($"{schema}.{table} must have RLS enabled");
            row.ForceRowSecurity.Should().BeTrue($"{schema}.{table} must FORCE RLS (the migrator role owns the table)");
        }
    }

    /// <summary>Two concurrent requests for the same (tenant, building, year) must never receive the
    /// same invoice sequence number — the atomic upsert+RETURNING in InvoiceSequenceRepository is
    /// what guarantees this; see plan §7.</summary>
    [Fact]
    public async Task Concurrent_GetNextValue_Calls_Never_Collide()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        int year = 2026;

        await using MyCondoDbContext dbA = _fixture.CreateDbContext(tenantId);
        await using MyCondoDbContext dbB = _fixture.CreateDbContext(tenantId);

        InvoiceSequenceRepository repoA = new(dbA);
        InvoiceSequenceRepository repoB = new(dbB);

        Task<int> taskA = repoA.GetNextValueAsync(tenantId, buildingId, year, CancellationToken.None);
        Task<int> taskB = repoB.GetNextValueAsync(tenantId, buildingId, year, CancellationToken.None);

        int[] results = await Task.WhenAll(taskA, taskB);

        results.Should().OnlyHaveUniqueItems();
        results.Should().BeEquivalentTo([1, 2]);
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
