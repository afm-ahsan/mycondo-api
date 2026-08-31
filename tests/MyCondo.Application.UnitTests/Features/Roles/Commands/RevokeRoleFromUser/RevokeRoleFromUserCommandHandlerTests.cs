using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Roles.Commands.RevokeRoleFromUser;

/// <summary>
/// Proves the "last holder" guard in <see cref="RevokeRoleFromUserCommandHandler"/>: revoking a
/// tenant-wide system role from its only remaining holder must be rejected, or a tenant could be left
/// with no one able to administer it. Building-scoped and non-system-role revocations don't carry this
/// restriction and must still succeed.
/// </summary>
public class RevokeRoleFromUserCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleAssignmentRepository _roleAssignments = Substitute.For<IRoleAssignmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly ITenantAdminProtectionService _tenantAdminProtection = Substitute.For<ITenantAdminProtectionService>();
    private readonly IIdentityAuditLogRepository _identityAuditLog = Substitute.For<IIdentityAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RevokeRoleFromUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private RevokeRoleFromUserCommandHandler CreateHandler() => new(
        _roles, _users, _roleAssignments, _unitOfWork, _currentUser, _tenantAdminProtection, _identityAuditLog, _clock,
        Substitute.For<ILogger<RevokeRoleFromUserCommandHandler>>());

    private static Role SystemRole() => Role.CreateSystem(RoleId.New(), TenantId, "SuperAdmin", "Full access", Now);

    private static User AUser() => User.Register(TenantId, "user@example.com", "hash", "A User", null, Now);

    [Fact]
    public async Task Throws_When_Revoking_A_Tenant_Wide_System_Role_From_Its_Last_Holder()
    {
        Role role = SystemRole();
        User user = AUser();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, null, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(assignment);
        _roleAssignments.LockAndCountTenantWideHoldersAsync(TenantId, role.Id, Arg.Any<CancellationToken>()).Returns(1);

        Func<Task> act = () => CreateHandler().Handle(
            new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        _roleAssignments.DidNotReceive().Remove(Arg.Any<RoleAssignment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeds_When_Another_Holder_Of_The_Same_Tenant_Wide_System_Role_Remains()
    {
        Role role = SystemRole();
        User user = AUser();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, null, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(assignment);
        _roleAssignments.LockAndCountTenantWideHoldersAsync(TenantId, role.Id, Arg.Any<CancellationToken>()).Returns(2);

        await CreateHandler().Handle(new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None);

        _roleAssignments.Received(1).Remove(assignment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Actor_Attempts_To_Self_Revoke_An_Admin_Equivalent_Role()
    {
        // mycondo-docs ADR-036 — revoking an admin-equivalent role from oneself is self-demotion and is
        // always rejected via EnsureCanMutateAdminTarget's self-protection, even with manageTenantAdmins.
        Role role = SystemRole();
        User user = AUser();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, null, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(assignment);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(true);
        _currentUser.UserId.Returns(user.Id.Value);
        _tenantAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, user.Id.Value, Arg.Any<bool>()))
            .Do(_ => throw new ForbiddenException("You cannot perform this action on your own account."));

        Func<Task> act = () => CreateHandler().Handle(
            new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        _roleAssignments.DidNotReceive().Remove(Arg.Any<RoleAssignment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Lower_Privileged_Actor_Revokes_An_Admin_Equivalent_Role_From_Another_User()
    {
        Role role = SystemRole();
        User user = AUser();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, null, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(assignment);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(false);
        _tenantAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, Arg.Any<Guid>(), false))
            .Do(_ => throw new ForbiddenException("Only a Tenant Admin can manage another Tenant Admin's account."));

        Func<Task> act = () => CreateHandler().Handle(
            new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        _roleAssignments.DidNotReceive().Remove(Arg.Any<RoleAssignment>());
        await _roleAssignments.DidNotReceive().LockAndCountTenantWideHoldersAsync(
            Arg.Any<Guid>(), Arg.Any<RoleId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeds_When_Actor_With_ManageTenantAdmins_Revokes_An_Admin_Role_From_Another_User_With_A_Holder_Remaining()
    {
        Role role = SystemRole();
        User user = AUser();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, null, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(assignment);
        _roleAssignments.LockAndCountTenantWideHoldersAsync(TenantId, role.Id, Arg.Any<CancellationToken>()).Returns(2);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(true);

        await CreateHandler().Handle(new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None);

        _roleAssignments.Received(1).Remove(assignment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_Not_Consult_Admin_Protection_When_Revoking_A_Non_Admin_Equivalent_Role()
    {
        Role role = SystemRole();
        User user = AUser();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, null, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(assignment);
        _roleAssignments.LockAndCountTenantWideHoldersAsync(TenantId, role.Id, Arg.Any<CancellationToken>()).Returns(2);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(false);

        await CreateHandler().Handle(new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None);

        _tenantAdminProtection.DidNotReceive().EnsureCanMutateAdminTarget(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>());
        _roleAssignments.Received(1).Remove(assignment);
    }

    [Fact]
    public async Task Succeeds_For_A_Building_Scoped_Assignment_Even_As_The_Only_Holder()
    {
        // The "last holder" guard only applies tenant-wide (BuildingId is null) — a building-scoped
        // assignment of the same system role never counts toward or blocks on it.
        Role role = SystemRole();
        User user = AUser();
        Guid buildingId = Guid.NewGuid();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, buildingId, Now);

        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetAsync(TenantId, user.Id, role.Id, buildingId, Arg.Any<CancellationToken>()).Returns(assignment);

        await CreateHandler().Handle(new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, buildingId), CancellationToken.None);

        _roleAssignments.Received(1).Remove(assignment);
        await _roleAssignments.DidNotReceive().LockAndCountTenantWideHoldersAsync(Arg.Any<Guid>(), Arg.Any<RoleId>(), Arg.Any<CancellationToken>());
    }
}
