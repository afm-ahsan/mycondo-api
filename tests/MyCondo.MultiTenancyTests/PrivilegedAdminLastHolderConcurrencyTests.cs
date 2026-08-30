using AwesomeAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Proves the race-safety claim in <c>ITenantAdminProtectionService.EnsureNotLastActiveAdminAsync</c>
/// and <c>IPlatformSuperAdminProtectionService.EnsureNotLastActiveSuperAdminAsync</c> (mycondo-docs
/// ADR-035) against a real PostgreSQL database — not mocks, which cannot exhibit the race at all.
///
/// The invariant these <c>FOR UPDATE</c> row-lock queries exist to protect: two concurrent requests
/// revoking an admin-equivalent role from the tenant's two remaining holders must not both observe
/// "2 holders remain" and both proceed — that would leave zero holders. This test opens two real,
/// concurrent database transactions and shows the second's lock query blocks until the first commits,
/// then reflects the post-commit state rather than a stale pre-commit snapshot — the actual mechanism
/// that makes the last-holder check correct under concurrency, not just in the mocked handler tests.
/// Requires a Docker daemon — see <see cref="MultiTenancyPostgresFixture"/>'s doc comment.
/// </summary>
public class PrivilegedAdminLastHolderConcurrencyTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly TimeSpan BlockedPollWindow = TimeSpan.FromMilliseconds(750);

    private readonly MultiTenancyPostgresFixture _fixture;

    public PrivilegedAdminLastHolderConcurrencyTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LockAndCountTenantWideHoldersAsync_Blocks_A_Concurrent_Reader_Until_The_First_Transaction_Commits()
    {
        Guid tenantId = Guid.NewGuid();
        Role adminRole = Role.CreateSystem(RoleId.New(), tenantId, "OrganizationAdmin", "Full access", Now, requiresBuildingScope: null);
        User holder1 = User.Register(tenantId, "holder1@example.com", "hash", "Holder One", null, Now);
        User holder2 = User.Register(tenantId, "holder2@example.com", "hash", "Holder Two", null, Now);
        RoleAssignment assignment1 = RoleAssignment.Grant(tenantId, holder1.Id, adminRole.Id, null, Now);
        RoleAssignment assignment2 = RoleAssignment.Grant(tenantId, holder2.Id, adminRole.Id, null, Now);

        await using (MyCondoDbContext seed = _fixture.CreateDbContext(tenantId))
        {
            seed.Set<Role>().Add(adminRole);
            seed.Set<User>().AddRange(holder1, holder2);
            seed.Set<RoleAssignment>().AddRange(assignment1, assignment2);
            await seed.SaveChangesAsync();
        }

        await using MyCondoDbContext dbTx1 = _fixture.CreateDbContext(tenantId);
        await using IDbContextTransaction tx1 = await dbTx1.Database.BeginTransactionAsync();
        RoleAssignmentRepository repoTx1 = new(dbTx1);

        // First actor locks and counts both holder rows (2), then revokes holder1 — but does not
        // commit yet, simulating the in-flight window of RevokeRoleFromUserCommandHandler's own
        // transaction between the lock query and SaveChangesAsync.
        int countSeenByTx1 = await repoTx1.LockAndCountTenantWideHoldersAsync(tenantId, adminRole.Id, CancellationToken.None);
        countSeenByTx1.Should().Be(2);
        RoleAssignment? trackedAssignment1 = await repoTx1.GetAsync(tenantId, holder1.Id, adminRole.Id, null, CancellationToken.None);
        repoTx1.Remove(trackedAssignment1!);
        await dbTx1.SaveChangesAsync();

        // Second actor concurrently attempts to revoke holder2 — its lock query must block on tx1's
        // held row locks rather than reading a stale "2 holders" snapshot.
        await using MyCondoDbContext dbTx2 = _fixture.CreateDbContext(tenantId);
        await using IDbContextTransaction tx2 = await dbTx2.Database.BeginTransactionAsync();
        RoleAssignmentRepository repoTx2 = new(dbTx2);

        Task<int> tx2LockTask = repoTx2.LockAndCountTenantWideHoldersAsync(tenantId, adminRole.Id, CancellationToken.None);
        Task firstToFinish = await Task.WhenAny(tx2LockTask, Task.Delay(BlockedPollWindow));

        firstToFinish.Should().NotBe(tx2LockTask, "tx2's FOR UPDATE query must block while tx1 still holds the row lock uncommitted");

        await tx1.CommitAsync();

        int countSeenByTx2 = await tx2LockTask;
        countSeenByTx2.Should().Be(1, "once unblocked, tx2 must see the post-commit state (holder1 already revoked), not the stale pre-commit count of 2");

        await tx2.CommitAsync();
    }

    [Fact]
    public async Task LockAndCountActiveSuperAdminHoldersAsync_Blocks_A_Concurrent_Reader_Until_The_First_Transaction_Commits()
    {
        PlatformRole superAdminRole = PlatformRole.CreateSystem(PlatformRoleId.New(), "SuperAdmin", "Full platform access", Now);
        PlatformUser holder1 = PlatformUser.Create("super1@mycondo.internal", "hash", "Super One", Now);
        PlatformUser holder2 = PlatformUser.Create("super2@mycondo.internal", "hash", "Super Two", Now);
        PlatformUserRoleAssignment assignment1 = PlatformUserRoleAssignment.Grant(holder1.Id, superAdminRole.Id, Now);
        PlatformUserRoleAssignment assignment2 = PlatformUserRoleAssignment.Grant(holder2.Id, superAdminRole.Id, Now);

        await using (MyCondoDbContext seed = _fixture.CreateDbContext(tenantId: null))
        {
            seed.Set<PlatformRole>().Add(superAdminRole);
            seed.Set<PlatformUser>().AddRange(holder1, holder2);
            seed.Set<PlatformUserRoleAssignment>().AddRange(assignment1, assignment2);
            await seed.SaveChangesAsync();
        }

        await using MyCondoDbContext dbTx1 = _fixture.CreateDbContext(tenantId: null);
        await using IDbContextTransaction tx1 = await dbTx1.Database.BeginTransactionAsync();
        PlatformUserRoleAssignmentRepository repoTx1 = new(dbTx1);

        int countSeenByTx1 = await repoTx1.LockAndCountActiveSuperAdminHoldersAsync(superAdminRole.Id, CancellationToken.None);
        countSeenByTx1.Should().Be(2);

        List<PlatformUserRoleAssignment> holder1Assignments = await repoTx1.GetForUserAsync(holder1.Id, CancellationToken.None);
        repoTx1.Remove(holder1Assignments.Single());
        await dbTx1.SaveChangesAsync();

        await using MyCondoDbContext dbTx2 = _fixture.CreateDbContext(tenantId: null);
        await using IDbContextTransaction tx2 = await dbTx2.Database.BeginTransactionAsync();
        PlatformUserRoleAssignmentRepository repoTx2 = new(dbTx2);

        Task<int> tx2LockTask = repoTx2.LockAndCountActiveSuperAdminHoldersAsync(superAdminRole.Id, CancellationToken.None);
        Task firstToFinish = await Task.WhenAny(tx2LockTask, Task.Delay(BlockedPollWindow));

        firstToFinish.Should().NotBe(tx2LockTask, "tx2's FOR UPDATE query must block while tx1 still holds the row lock uncommitted");

        await tx1.CommitAsync();

        int countSeenByTx2 = await tx2LockTask;
        countSeenByTx2.Should().Be(1, "once unblocked, tx2 must see the post-commit state (holder1's assignment already revoked), not the stale pre-commit count of 2");

        await tx2.CommitAsync();
    }
}
