using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for property.flat_ownerships (Phase 3, mycondo-docs ADR-021) — same
/// pattern as CrossTenantIsolationTests, scoped to the new table. Requires a Docker daemon; not
/// executed in the environment this was authored in — see MultiTenancyPostgresFixture's doc comment.
/// </summary>
public class FlatOwnershipRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public FlatOwnershipRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FlatOwnerships_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        FlatId flatA = FlatId.New();
        FlatId flatB = FlatId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<FlatOwnership>().Add(FlatOwnership.Grant(
                tenantA, Guid.NewGuid(), flatA, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<FlatOwnership>().Add(FlatOwnership.Grant(
                tenantB, Guid.NewGuid(), flatB, DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<FlatOwnership> visible = await asTenantA.Set<FlatOwnership>().ToListAsync();
            visible.Should().ContainSingle(o => o.FlatId == flatA);
            visible.Should().NotContain(o => o.FlatId == flatB);
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<FlatOwnership> visible = await noTenant.Set<FlatOwnership>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's ownership rows");
        }
    }

    [Fact]
    public async Task Insert_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it.
        dbAsTenantB.Set<FlatOwnership>().Add(FlatOwnership.Grant(
            tenantA, Guid.NewGuid(), FlatId.New(), DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task FlatOwnerships_Table_Has_Rls_Enabled_And_Forced()
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        RlsFlags row = await db.Database
            .SqlQuery<RlsFlags>(
                $"""
                SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                FROM pg_class
                WHERE oid = 'property.flat_ownerships'::regclass
                """)
            .SingleAsync();

        row.RowSecurity.Should().BeTrue("property.flat_ownerships must have RLS enabled");
        row.ForceRowSecurity.Should().BeTrue("property.flat_ownerships must FORCE RLS (the app's DB role owns the table)");
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
