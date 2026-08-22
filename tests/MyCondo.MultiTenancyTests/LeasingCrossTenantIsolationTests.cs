using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the new `leasing` schema tables (Tenant Registration). Same pattern
/// as <see cref="AmenitiesCrossTenantIsolationTests"/>. Requires a Docker daemon. Written and reviewed
/// for correctness but NOT executed in the environment they were authored in (Docker was unavailable)
/// — run wherever Docker is actually available before trusting them.
/// </summary>
public class LeasingCrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public LeasingCrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OccupancyRegistrations_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        FlatId flatA = FlatId.New();
        FlatId flatB = FlatId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<OccupancyRegistration>().Add(OccupancyRegistration.Register(
                tenantA, flatA, ResidentId.New(), ResidentType.Occupant, "Tenant A Occupant", null, null, null, null,
                null, null, null, null, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<OccupancyRegistration>().Add(OccupancyRegistration.Register(
                tenantB, flatB, ResidentId.New(), ResidentType.Occupant, "Tenant B Occupant", null, null, null, null,
                null, null, null, null, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<OccupancyRegistration> visible = await asTenantA.Set<OccupancyRegistration>().ToListAsync();
            visible.Should().ContainSingle(r => r.PrimaryFullName == "Tenant A Occupant");
            visible.Should().NotContain(r => r.PrimaryFullName == "Tenant B Occupant");
        }
    }

    [Fact]
    public async Task Insert_OccupancyRegistration_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        dbAsTenantB.Set<OccupancyRegistration>().Add(OccupancyRegistration.Register(
            tenantA, FlatId.New(), ResidentId.New(), ResidentType.Occupant, "Impersonator", null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Leasing_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("leasing", "occupancy_registrations"),
            ("leasing", "household_members"),
            ("leasing", "occupancy_registration_status_histories"),
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
