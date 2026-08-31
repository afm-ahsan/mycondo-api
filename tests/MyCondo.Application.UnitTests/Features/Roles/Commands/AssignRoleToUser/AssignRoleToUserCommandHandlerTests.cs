using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Roles.Commands.AssignRoleToUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Roles.Commands.AssignRoleToUser;

/// <summary>
/// Proves the ADR-036 grant-time gate in <see cref="AssignRoleToUserCommandHandler"/>: granting a
/// tenant-wide, admin-equivalent system role — including granting it to oneself — requires
/// <c>user.manageTenantAdmins</c> on top of the base <c>role.manage</c> permission already enforced at
/// the endpoint filter. Also proves cross-tenant IDOR is closed for the role, building, and user lookups,
/// and that every successful grant records an <see cref="IdentityAuditLogEntry"/>.
/// </summary>
public class AssignRoleToUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IRoleAssignmentRepository _roleAssignments = Substitute.For<IRoleAssignmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly ITenantAdminProtectionService _tenantAdminProtection = Substitute.For<ITenantAdminProtectionService>();
    private readonly IIdentityAuditLogRepository _identityAuditLog = Substitute.For<IIdentityAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AssignRoleToUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private AssignRoleToUserCommandHandler CreateHandler() => new(
        _roles, _users, _buildings, _roleAssignments, _unitOfWork, _currentUser, _tenantAdminProtection,
        _identityAuditLog, _clock, Substitute.For<ILogger<AssignRoleToUserCommandHandler>>());

    private static Role AdminEquivalentRole() =>
        Role.CreateSystem(RoleId.New(), TenantId, "OrganizationAdmin", "Full access", Now, requiresBuildingScope: null);

    private static Role OrdinaryRole() => Role.CreateCustom(TenantId, "Member", "Ordinary role", Now);

    private static User AUser(Guid tenantId) => User.Register(tenantId, "user@example.com", "hash", "A User", null, Now);

    private void SetUpNoDuplicate(Role role, User user) =>
        _roleAssignments.ExistsAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(false);

    [Fact]
    public async Task Throws_Forbidden_When_Granting_An_Admin_Equivalent_Role_Without_ManageTenantAdmins()
    {
        Role role = AdminEquivalentRole();
        User user = AUser(TenantId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(true);
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(false);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignRoleToUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        _roleAssignments.DidNotReceive().Add(Arg.Any<RoleAssignment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Actor_Attempts_To_Self_Grant_An_Admin_Equivalent_Role()
    {
        // Self-promotion is not exempt from the manageTenantAdmins gate — unlike EnsureCanMutateAdminTarget's
        // self-protection carve-out (which only ever forbids self-action), a grant is denied outright to
        // anyone lacking the permission, target included.
        Role role = AdminEquivalentRole();
        Guid actorId = Guid.NewGuid();
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(true);
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(false);
        _currentUser.UserId.Returns(actorId);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignRoleToUserCommand(role.Id.Value, actorId, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Succeeds_Granting_An_Admin_Equivalent_Role_When_Actor_Has_ManageTenantAdmins()
    {
        Role role = AdminEquivalentRole();
        User user = AUser(TenantId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(true);
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(true);
        SetUpNoDuplicate(role, user);

        await CreateHandler().Handle(new AssignRoleToUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None);

        _roleAssignments.Received(1).Add(Arg.Any<RoleAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _identityAuditLog.Received(1).Add(Arg.Is<IdentityAuditLogEntry>(e =>
            e.Action == "User.Role.Assign" && e.TargetId == user.Id.Value.ToString()));
    }

    [Fact]
    public async Task Succeeds_Granting_A_Non_Admin_Role_Without_ManageTenantAdmins()
    {
        Role role = OrdinaryRole();
        User user = AUser(TenantId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(false);
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(false);
        SetUpNoDuplicate(role, user);

        await CreateHandler().Handle(new AssignRoleToUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None);

        _roleAssignments.Received(1).Add(Arg.Any<RoleAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Role_Belongs_To_A_Different_Tenant()
    {
        Role role = Role.CreateCustom(OtherTenantId, "Foreign Role", "", Now);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignRoleToUserCommand(role.Id.Value, Guid.NewGuid(), null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Target_User_Belongs_To_A_Different_Tenant()
    {
        Role role = OrdinaryRole();
        User foreignUser = AUser(OtherTenantId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(false);
        _users.GetByIdAsync(foreignUser.Id, Arg.Any<CancellationToken>()).Returns(foreignUser);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignRoleToUserCommand(role.Id.Value, foreignUser.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Building_Belongs_To_A_Different_Tenant()
    {
        Role role = Role.CreateSystem(RoleId.New(), TenantId, "CondoAdmin", "", Now, requiresBuildingScope: true);
        Building foreignBuilding = Building.Create(OtherTenantId, "Tower A", "TWR-A", null, Now);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(false);
        _buildings.GetByIdAsync(foreignBuilding.Id, Arg.Any<CancellationToken>()).Returns(foreignBuilding);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignRoleToUserCommand(role.Id.Value, Guid.NewGuid(), foreignBuilding.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_The_User_Already_Holds_The_Role_For_This_Scope()
    {
        Role role = OrdinaryRole();
        User user = AUser(TenantId);
        _roles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tenantAdminProtection.IsTenantAdminEquivalent(role).Returns(false);
        _roleAssignments.ExistsAsync(TenantId, user.Id, role.Id, null, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignRoleToUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        _roleAssignments.DidNotReceive().Add(Arg.Any<RoleAssignment>());
    }
}
