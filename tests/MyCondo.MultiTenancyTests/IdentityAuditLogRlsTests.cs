using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for <c>identity.identity_audit_log</c> (mycondo-docs ADR-036), same
/// pattern as <see cref="FinanceRlsTests"/>'s <c>FinanceAuditLog_Cross_Tenant_Isolation</c> and
/// <c>Finance_Tables_Have_Rls_Enabled_And_Forced</c>. A tenant's privileged-user-management audit trail
/// must be exactly as isolated as its finance audit trail — an attacker who compromises one tenant's
/// session must never be able to read another tenant's user/role administration history. Requires a
/// Docker daemon — see <see cref="MultiTenancyPostgresFixture"/>'s doc comment.
/// </summary>
public class IdentityAuditLogRlsTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public IdentityAuditLogRlsTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IdentityAuditLog_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<IdentityAuditLogEntry>().Add(
                IdentityAuditLogEntry.Record(tenantA, DateTimeOffset.UtcNow, null, "User.Deactivate"));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<IdentityAuditLogEntry>().Add(
                IdentityAuditLogEntry.Record(tenantB, DateTimeOffset.UtcNow, null, "User.Role.Assign"));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<IdentityAuditLogEntry> visible = await asTenantA.Set<IdentityAuditLogEntry>().ToListAsync();
            visible.Should().ContainSingle(e => e.Action == "User.Deactivate");
            visible.Should().NotContain(e => e.Action == "User.Role.Assign");
        }

        await using (MyCondoDbContext noTenant = _fixture.CreateDbContext(tenantId: null))
        {
            List<IdentityAuditLogEntry> visible = await noTenant.Set<IdentityAuditLogEntry>().ToListAsync();
            visible.Should().BeEmpty("a connection with no tenant context set must default-deny, not see every tenant's identity audit log");
        }
    }

    [Fact]
    public async Task Insert_IdentityAuditLogEntry_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        // Row claims tenantA while the connection's context is tenantB — WITH CHECK must reject it,
        // closing the same forged-tenant-id write path FinanceRlsTests proves for finance_audit_log.
        dbAsTenantB.Set<IdentityAuditLogEntry>().Add(
            IdentityAuditLogEntry.Record(tenantA, DateTimeOffset.UtcNow, null, "User.Create"));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task IdentityAuditLog_Table_Has_Rls_Enabled_And_Forced()
    {
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        RlsFlags row = await db.Database
            .SqlQuery<RlsFlags>(
                $"""
                SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                FROM pg_class
                WHERE oid = 'identity.identity_audit_log'::regclass
                """)
            .SingleAsync();

        row.RowSecurity.Should().BeTrue("identity.identity_audit_log must have RLS enabled");
        row.ForceRowSecurity.Should().BeTrue("identity.identity_audit_log must FORCE RLS (the app's DB role owns the table)");
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
