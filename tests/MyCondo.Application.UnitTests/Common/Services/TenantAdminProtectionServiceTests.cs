using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Unit tests for <see cref="TenantAdminProtectionService"/> (mycondo-docs ADR-036) — the handler-level
/// gate for tenant-wide, admin-equivalent system roles. Covers the "tenant-admin-equivalent" predicate,
/// self-protection, the manageTenantAdmins composition permission, and the last-active-admin invariant.
/// </summary>
public class TenantAdminProtectionServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IRoleAssignmentRepository _roleAssignments = Substitute.For<IRoleAssignmentRepository>();

    private TenantAdminProtectionService CreateService() => new(_roles, _roleAssignments);

    private static Role TenantWideSystemRole(string name = "OrganizationAdmin") =>
        Role.CreateSystem(RoleId.New(), TenantId, name, "Full access", Now, requiresBuildingScope: null);

    private static Role BuildingScopedSystemRole() =>
        Role.CreateSystem(RoleId.New(), TenantId, "CondoAdmin", "Building admin", Now, requiresBuildingScope: true);

    private static Role NonSystemRole() =>
        Role.CreateCustom(TenantId, "Custom Role", "Not a system role", Now);

    // --- IsTenantAdminEquivalent ------------------------------------------------------------------

    [Fact]
    public void IsTenantAdminEquivalent_True_For_System_Role_With_No_Building_Scope_Requirement()
    {
        CreateService().IsTenantAdminEquivalent(TenantWideSystemRole()).Should().BeTrue();
    }

    [Fact]
    public void IsTenantAdminEquivalent_False_For_Building_Scoped_System_Role()
    {
        CreateService().IsTenantAdminEquivalent(BuildingScopedSystemRole()).Should().BeFalse();
    }

    [Fact]
    public void IsTenantAdminEquivalent_False_For_Non_System_Role()
    {
        CreateService().IsTenantAdminEquivalent(NonSystemRole()).Should().BeFalse();
    }

    // --- TargetHoldsTenantAdminRoleAsync -----------------------------------------------------------

    [Fact]
    public async Task TargetHoldsTenantAdminRoleAsync_True_When_Any_Assignment_Is_Admin_Equivalent()
    {
        UserId userId = UserId.New();
        Role adminRole = TenantWideSystemRole();
        Role buildingRole = BuildingScopedSystemRole();
        List<RoleAssignment> assignments =
        [
            RoleAssignment.Grant(TenantId, userId, buildingRole.Id, Guid.NewGuid(), Now),
            RoleAssignment.Grant(TenantId, userId, adminRole.Id, null, Now),
        ];
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns(assignments);
        _roles.GetByIdAsync(buildingRole.Id, Arg.Any<CancellationToken>()).Returns(buildingRole);
        _roles.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);

        bool result = await CreateService().TargetHoldsTenantAdminRoleAsync(TenantId, userId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TargetHoldsTenantAdminRoleAsync_False_When_No_Assignment_Is_Admin_Equivalent()
    {
        UserId userId = UserId.New();
        Role buildingRole = BuildingScopedSystemRole();
        List<RoleAssignment> assignments = [RoleAssignment.Grant(TenantId, userId, buildingRole.Id, Guid.NewGuid(), Now)];
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns(assignments);
        _roles.GetByIdAsync(buildingRole.Id, Arg.Any<CancellationToken>()).Returns(buildingRole);

        bool result = await CreateService().TargetHoldsTenantAdminRoleAsync(TenantId, userId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TargetHoldsTenantAdminRoleAsync_False_When_User_Has_No_Assignments()
    {
        UserId userId = UserId.New();
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns([]);

        bool result = await CreateService().TargetHoldsTenantAdminRoleAsync(TenantId, userId, CancellationToken.None);

        result.Should().BeFalse();
    }

    // --- EnsureCanMutateAdminTarget (destructive actions: deactivate, revoke admin role) ------------

    [Fact]
    public void EnsureCanMutateAdminTarget_Throws_When_Actor_Targets_Own_Account_Even_With_Permission()
    {
        Guid actorId = Guid.NewGuid();

        Action act = () => CreateService().EnsureCanMutateAdminTarget(actorId, actorId, actorCanManageTenantAdmins: true);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureCanMutateAdminTarget_Throws_When_Actor_Lacks_ManageTenantAdmins()
    {
        Action act = () => CreateService().EnsureCanMutateAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageTenantAdmins: false);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureCanMutateAdminTarget_Succeeds_For_Different_Target_With_Permission()
    {
        Action act = () => CreateService().EnsureCanMutateAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageTenantAdmins: true);

        act.Should().NotThrow();
    }

    // --- EnsureCanEditAdminTarget (non-destructive edits: update profile, re-activate) ---------------

    [Fact]
    public void EnsureCanEditAdminTarget_Allows_Self_Edit_Without_Permission()
    {
        Guid actorId = Guid.NewGuid();

        Action act = () => CreateService().EnsureCanEditAdminTarget(actorId, actorId, actorCanManageTenantAdmins: false);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEditAdminTarget_Throws_When_Editing_Another_Admin_Without_Permission()
    {
        Action act = () => CreateService().EnsureCanEditAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageTenantAdmins: false);

        act.Should().Throw<ForbiddenException>();
    }

    [Fact]
    public void EnsureCanEditAdminTarget_Succeeds_When_Editing_Another_Admin_With_Permission()
    {
        Action act = () => CreateService().EnsureCanEditAdminTarget(
            Guid.NewGuid(), Guid.NewGuid(), actorCanManageTenantAdmins: true);

        act.Should().NotThrow();
    }

    // --- EnsureNotLastActiveAdminAsync (race-safe last-holder invariant) -----------------------------

    [Fact]
    public async Task EnsureNotLastActiveAdminAsync_Throws_When_Target_Is_The_Sole_Tenant_Wide_Holder()
    {
        UserId userId = UserId.New();
        Role adminRole = TenantWideSystemRole();
        List<RoleAssignment> assignments = [RoleAssignment.Grant(TenantId, userId, adminRole.Id, null, Now)];
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns(assignments);
        _roles.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        _roleAssignments.LockAndCountTenantWideHoldersAsync(TenantId, adminRole.Id, Arg.Any<CancellationToken>()).Returns(1);

        Func<Task> act = () => CreateService().EnsureNotLastActiveAdminAsync(TenantId, userId, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task EnsureNotLastActiveAdminAsync_Succeeds_When_Another_Holder_Remains()
    {
        UserId userId = UserId.New();
        Role adminRole = TenantWideSystemRole();
        List<RoleAssignment> assignments = [RoleAssignment.Grant(TenantId, userId, adminRole.Id, null, Now)];
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns(assignments);
        _roles.GetByIdAsync(adminRole.Id, Arg.Any<CancellationToken>()).Returns(adminRole);
        _roleAssignments.LockAndCountTenantWideHoldersAsync(TenantId, adminRole.Id, Arg.Any<CancellationToken>()).Returns(2);

        Func<Task> act = () => CreateService().EnsureNotLastActiveAdminAsync(TenantId, userId, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureNotLastActiveAdminAsync_Ignores_Building_Scoped_Assignments_Of_The_Same_Role()
    {
        // A building-scoped assignment of a tenant-wide-capable role must never be treated as a
        // tenant-wide holder — the last-holder guard only ever counts BuildingId == null assignments.
        UserId userId = UserId.New();
        Role adminRole = TenantWideSystemRole();
        List<RoleAssignment> assignments = [RoleAssignment.Grant(TenantId, userId, adminRole.Id, Guid.NewGuid(), Now)];
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns(assignments);

        Func<Task> act = () => CreateService().EnsureNotLastActiveAdminAsync(TenantId, userId, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _roleAssignments.DidNotReceive().LockAndCountTenantWideHoldersAsync(
            Arg.Any<Guid>(), Arg.Any<RoleId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureNotLastActiveAdminAsync_Ignores_Non_Admin_Equivalent_Roles()
    {
        UserId userId = UserId.New();
        Role buildingRole = BuildingScopedSystemRole();
        List<RoleAssignment> assignments = [RoleAssignment.Grant(TenantId, userId, buildingRole.Id, null, Now)];
        _roleAssignments.GetForUserAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns(assignments);
        _roles.GetByIdAsync(buildingRole.Id, Arg.Any<CancellationToken>()).Returns(buildingRole);

        Func<Task> act = () => CreateService().EnsureNotLastActiveAdminAsync(TenantId, userId, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _roleAssignments.DidNotReceive().LockAndCountTenantWideHoldersAsync(
            Arg.Any<Guid>(), Arg.Any<RoleId>(), Arg.Any<CancellationToken>());
    }
}
