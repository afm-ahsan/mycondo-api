using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the Slice F `utilities` schema tables (meters, meter_assignments,
/// rate_plans, rate_slabs, readings) — same pattern as <see cref="BillingCrossTenantIsolationTests"/>.
/// Requires a Docker daemon. Written and reviewed for correctness but NOT executed in the
/// environment they were authored in — run wherever Docker is actually available before trusting
/// them.
/// </summary>
public class UtilitiesCrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public UtilitiesCrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Meters_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        BuildingId buildingA = BuildingId.New();
        BuildingId buildingB = BuildingId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Meter>().Add(Meter.Install(tenantA, buildingA, UtilityType.Electricity, "MTR-A-001", DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Meter>().Add(Meter.Install(tenantB, buildingB, UtilityType.Electricity, "MTR-B-001", DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Meter> visible = await asTenantA.Set<Meter>().ToListAsync();
            visible.Should().ContainSingle(m => m.MeterNumber == "MTR-A-001");
            visible.Should().NotContain(m => m.MeterNumber == "MTR-B-001");
        }
    }

    [Fact]
    public async Task Insert_Meter_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        dbAsTenantB.Set<Meter>().Add(Meter.Install(tenantA, BuildingId.New(), UtilityType.Gas, "MTR-IMPERSONATOR", DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Slice_F_Utilities_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("utilities", "meters"),
            ("utilities", "meter_assignments"),
            ("utilities", "rate_plans"),
            ("utilities", "rate_slabs"),
            ("utilities", "readings"),
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

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
