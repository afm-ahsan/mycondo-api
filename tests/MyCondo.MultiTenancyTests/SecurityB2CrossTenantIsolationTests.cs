using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Payroll.StaffMembers;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the Slice B2 tables (domestic worker/service provider
/// profiles+assignments, seba visit details, staff members, attendance records) — same pattern as
/// <see cref="SecurityCrossTenantIsolationTests"/>. Requires a Docker daemon. Written and reviewed for
/// correctness but NOT executed in the environment they were authored in — run wherever Docker is
/// actually available before trusting them.
/// </summary>
public class SecurityB2CrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public SecurityB2CrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DomesticWorkerProfiles_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<DomesticWorkerProfile>().Add(DomesticWorkerProfile.Register(
                tenantA, "Worker A", "01700000001", DomesticWorkerType.Maid, null, null, null, null, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<DomesticWorkerProfile>().Add(DomesticWorkerProfile.Register(
                tenantB, "Worker B", "01700000002", DomesticWorkerType.Maid, null, null, null, null, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<DomesticWorkerProfile> visible = await asTenantA.Set<DomesticWorkerProfile>().ToListAsync();
            visible.Should().ContainSingle(w => w.FullName == "Worker A");
            visible.Should().NotContain(w => w.FullName == "Worker B");
        }
    }

    [Fact]
    public async Task Insert_StaffMember_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        dbAsTenantB.Set<StaffMember>().Add(StaffMember.Register(
            tenantA, "Impersonator", StaffRole.Guard, null, DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Slice_B2_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("security", "domestic_worker_profiles"),
            ("security", "domestic_worker_assignments"),
            ("security", "service_provider_profiles"),
            ("security", "service_provider_assignments"),
            ("security", "seba_visit_details"),
            ("payroll", "staff_members"),
            ("payroll", "attendance_records"),
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
