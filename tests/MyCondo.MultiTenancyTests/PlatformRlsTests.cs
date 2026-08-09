using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Verifies the Phase 1 RLS boundary claim from mycondo-docs ADR-019: platform.* tables carry no
/// tenant_id and correctly have NO RLS policy (same justification already applies to tenancy.tenants),
/// and — the more important direction — introducing them does not touch or weaken any existing
/// tenant-scoped table's RLS in any way.
///
/// Requires a Docker daemon. Written and reviewed for correctness but NOT executed in the environment
/// this was authored in — see MultiTenancyPostgresFixture's doc comment. Run wherever Docker is
/// actually available before trusting this as currently passing.
/// </summary>
public class PlatformRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public PlatformRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("platform_users")]
    [InlineData("platform_roles")]
    [InlineData("platform_role_permissions")]
    [InlineData("platform_user_role_assignments")]
    [InlineData("platform_refresh_tokens")]
    [InlineData("platform_audit_log")]
    public async Task Platform_Tables_Deliberately_Have_No_Rls_Policy(string table)
    {
        // A positive assertion, not just an absence of a negative test — so a future engineer who
        // "fixes" this by adding RLS notices the assertion fail and re-reads the doc comment above,
        // rather than silently reintroducing a tenant_id these tables were never meant to have.
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        RlsFlags row = await db.Database
            .SqlQuery<RlsFlags>(
                $"""
                SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                FROM pg_class
                WHERE oid = ('platform.' || {table})::regclass
                """)
            .SingleAsync();

        row.RowSecurity.Should().BeFalse($"platform.{table} holds no tenant-scoped data and must not have RLS enabled");
        row.ForceRowSecurity.Should().BeFalse($"platform.{table} holds no tenant-scoped data and must not FORCE RLS");
    }

    [Fact]
    public async Task Existing_Tenant_Rls_Is_Unaffected_By_Introducing_Platform_Tables()
    {
        // Regression guard for the approved Phase 1 invariant "existing PostgreSQL tenant RLS remains
        // unchanged/FORCE-enabled" — re-runs the pre-existing identity-schema RLS assertion against a
        // database that has now also had the platform migrations applied.
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

            row.RowSecurity.Should().BeTrue($"identity.{table} must still have RLS enabled after the platform migrations");
            row.ForceRowSecurity.Should().BeTrue($"identity.{table} must still FORCE RLS after the platform migrations");
        }
    }

    [Fact]
    public async Task A_Platform_Scoped_Connection_Still_Sees_Zero_Tenant_Rows()
    {
        // Belt-and-suspenders defense-in-depth proof: even setting up a MyCondoDbContext exactly the
        // way a platform-authenticated request would (no tenant context at all, since PlatformUser has
        // no TenantId to read one from) still hits the existing fail-closed RLS behavior for any
        // tenant-scoped table it might — incorrectly — be asked to query.
        await using MyCondoDbContext platformScoped = _fixture.CreateDbContext(tenantId: null);

        List<MyCondo.Domain.Features.Identity.Users.User> visible =
            await platformScoped.Set<MyCondo.Domain.Features.Identity.Users.User>().ToListAsync();

        visible.Should().BeEmpty("a Platform-scope connection has no tenant context and must not see any tenant's users");
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
