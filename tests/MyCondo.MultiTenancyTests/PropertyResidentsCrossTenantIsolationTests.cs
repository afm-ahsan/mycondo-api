using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the Slice A tables (property.buildings, property.flats,
/// property.gates, residents.residents, documents.attachments) — same pattern as
/// <see cref="CrossTenantIsolationTests"/> for the identity schema. Requires a Docker daemon. Written
/// and reviewed for correctness but NOT executed in the environment they were authored in (no Docker
/// daemon available there) — run wherever Docker is actually available before trusting them.
/// </summary>
public class PropertyResidentsCrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public PropertyResidentsCrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Buildings_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Building>().Add(Building.Create(tenantA, "Tower A", "TWRA", null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Building>().Add(Building.Create(tenantB, "Tower B", "TWRB", null, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Building> visible = await asTenantA.Set<Building>().ToListAsync();
            visible.Should().ContainSingle(b => b.Name == "Tower A");
            visible.Should().NotContain(b => b.Name == "Tower B");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<Building> visible = await noTenant.Set<Building>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's rows");
        }
    }

    [Fact]
    public async Task Insert_Building_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it.
        dbAsTenantB.Set<Building>().Add(Building.Create(tenantA, "Impersonator Tower", "IMP", null, DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Slice_A_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("property", "buildings"),
            ("property", "flats"),
            ("property", "gates"),
            ("residents", "residents"),
            ("documents", "attachments"),
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
