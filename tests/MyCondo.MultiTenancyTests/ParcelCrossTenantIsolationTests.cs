using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Parcels;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the Slice C tables (security.parcels,
/// security.parcel_custody_events) — same pattern as <see cref="SecurityCrossTenantIsolationTests"/>.
/// Requires a Docker daemon. Written and reviewed for correctness but NOT executed in the environment
/// they were authored in — run wherever Docker is actually available before trusting them.
/// </summary>
public class ParcelCrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public ParcelCrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Parcels_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        FlatId flatA = FlatId.New();
        FlatId flatB = FlatId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Parcel>().Add(Parcel.Receive(
                tenantA, "REF-A", null, null, null, flatA, null, ParcelType.Package, 1, null, null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Parcel>().Add(Parcel.Receive(
                tenantB, "REF-B", null, null, null, flatB, null, ParcelType.Package, 1, null, null, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Parcel> visible = await asTenantA.Set<Parcel>().ToListAsync();
            visible.Should().ContainSingle(p => p.ParcelReference == "REF-A");
            visible.Should().NotContain(p => p.ParcelReference == "REF-B");
        }
    }

    [Fact]
    public async Task Insert_Parcel_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        dbAsTenantB.Set<Parcel>().Add(Parcel.Receive(
            tenantA, "REF-IMPERSONATOR", null, null, null, FlatId.New(), null, ParcelType.Package, 1, null, null,
            DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Slice_C_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("security", "parcels"),
            ("security", "parcel_custody_events"),
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
