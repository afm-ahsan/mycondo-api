using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;
using MyCondo.Domain.Abstractions;
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

    public RevokeRoleFromUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private RevokeRoleFromUserCommandHandler CreateHandler() => new(
        _roles, _users, _roleAssignments, _unitOfWork, _currentUser,
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
        _roleAssignments.CountTenantWideHoldersAsync(TenantId, role.Id, Arg.Any<CancellationToken>()).Returns(1);

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
        _roleAssignments.CountTenantWideHoldersAsync(TenantId, role.Id, Arg.Any<CancellationToken>()).Returns(2);

        await CreateHandler().Handle(new RevokeRoleFromUserCommand(role.Id.Value, user.Id.Value, null), CancellationToken.None);

        _roleAssignments.Received(1).Remove(assignment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
        await _roleAssignments.DidNotReceive().CountTenantWideHoldersAsync(Arg.Any<Guid>(), Arg.Any<RoleId>(), Arg.Any<CancellationToken>());
    }
}
