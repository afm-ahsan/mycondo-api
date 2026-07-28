using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests against a Testcontainers-backed PostgreSQL — see
/// mycondo-docs/02-architecture/Architecture_Decision_Register.md ADR-009 and
/// mycondo-docs/07-delivery/MASTER_BACKLOG.md MT-2/MT-3/MT-4.
///
/// Requires a Docker daemon. Written and reviewed for correctness but NOT executed in the
/// environment they were authored in — see MultiTenancyPostgresFixture's doc comment. Run wherever
/// Docker is actually available before trusting them.
/// </summary>
public class CrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public CrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Users_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<User>().Add(User.Register(
                tenantA, "owner-a@example.com", "hash", "Owner A", null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<User>().Add(User.Register(
                tenantB, "owner-b@example.com", "hash", "Owner B", null, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<User> visible = await asTenantA.Set<User>().ToListAsync();
            visible.Should().ContainSingle(u => u.Email == "owner-a@example.com");
            visible.Should().NotContain(u => u.Email == "owner-b@example.com");
        }

        await using (MyCondoDbContext asTenantB = _fixture.CreateDbContext(tenantB))
        {
            List<User> visible = await asTenantB.Set<User>().ToListAsync();
            visible.Should().ContainSingle(u => u.Email == "owner-b@example.com");
            visible.Should().NotContain(u => u.Email == "owner-a@example.com");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<User> visible = await noTenant.Set<User>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's rows");
        }
    }

    [Fact]
    public async Task Insert_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it.
        dbAsTenantB.Set<User>().Add(User.Register(
            tenantA, "impersonator@example.com", "hash", "Impersonator", null, DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Expected_Tables_Have_Rls_Enabled_And_Forced()
    {
        string[] tables = ["users", "roles", "role_assignments", "refresh_tokens", "role_permissions"];

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        foreach (string table in tables)
        {
            RlsFlags row = await db.Database
                .SqlQuery<RlsFlags>(
                    $"""
                    SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                    FROM pg_class
                    WHERE oid = ('identity.' || {table})::regclass
                    """)
                .SingleAsync();

            row.RowSecurity.Should().BeTrue($"identity.{table} must have RLS enabled");
            row.ForceRowSecurity.Should().BeTrue($"identity.{table} must FORCE RLS (the app's DB role owns the table)");
        }
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
