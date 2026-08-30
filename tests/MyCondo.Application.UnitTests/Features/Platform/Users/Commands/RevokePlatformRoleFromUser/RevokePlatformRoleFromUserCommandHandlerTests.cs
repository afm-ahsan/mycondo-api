using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.Commands.RevokePlatformRoleFromUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Users.Commands.RevokePlatformRoleFromUser;

/// <summary>
/// Platform-scope analogue of RevokeRoleFromUserCommandHandlerTests: revoking the SuperAdmin role
/// (demotion) is subject to self-protection, the manageSuperAdmins composition permission, and the
/// race-safe last-active-Super-Admin invariant (mycondo-docs ADR-035).
/// </summary>
public class RevokePlatformRoleFromUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPlatformRoleRepository _platformRoles = Substitute.For<IPlatformRoleRepository>();
    private readonly IPlatformUserRepository _platformUsers = Substitute.For<IPlatformUserRepository>();
    private readonly IPlatformUserRoleAssignmentRepository _assignments = Substitute.For<IPlatformUserRoleAssignmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentPlatformUserProvider _currentUser = Substitute.For<ICurrentPlatformUserProvider>();
    private readonly IPlatformSuperAdminProtectionService _superAdminProtection = Substitute.For<IPlatformSuperAdminProtectionService>();
    private readonly IPlatformAuditLogRepository _platformAuditLog = Substitute.For<IPlatformAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RevokePlatformRoleFromUserCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(Now);
    }

    private RevokePlatformRoleFromUserCommandHandler CreateHandler() => new(
        _platformRoles, _platformUsers, _assignments, _unitOfWork, _currentUser, _superAdminProtection,
        _platformAuditLog, _clock, Substitute.For<ILogger<RevokePlatformRoleFromUserCommandHandler>>());

    private static PlatformRole SuperAdminRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "SuperAdmin", "Full access", Now);

    private static PlatformRole SupportRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "Support", "Limited access", Now);

    private static PlatformUser AUser() => PlatformUser.Create("operator@mycondo.internal", "hash", "Operator", Now);

    private void SetUpAssignment(PlatformUser user, PlatformRole role, PlatformUserRoleAssignment assignment) =>
        _assignments.GetForUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns([assignment]);

    [Fact]
    public async Task Throws_When_Revoking_The_SuperAdmin_Role_From_Its_Last_Holder()
    {
        PlatformRole role = SuperAdminRole();
        PlatformUser user = AUser();
        PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(user.Id, role.Id, Now);

        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        SetUpAssignment(user, role, assignment);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(true);
        _superAdminProtection
            .EnsureNotLastActiveSuperAdminAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new ConflictException("Cannot remove this account — it is the last active Platform Super Admin."));

        Func<Task> act = () => CreateHandler().Handle(
            new RevokePlatformRoleFromUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        _assignments.DidNotReceive().Remove(Arg.Any<PlatformUserRoleAssignment>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Actor_Attempts_To_Self_Revoke_The_SuperAdmin_Role()
    {
        PlatformRole role = SuperAdminRole();
        PlatformUser user = AUser();
        PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(user.Id, role.Id, Now);

        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        SetUpAssignment(user, role, assignment);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.PlatformUserId.Returns(user.Id.Value);
        _superAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, user.Id.Value, Arg.Any<bool>()))
            .Do(_ => throw new ForbiddenException("You cannot perform this action on your own account."));

        Func<Task> act = () => CreateHandler().Handle(
            new RevokePlatformRoleFromUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        _assignments.DidNotReceive().Remove(Arg.Any<PlatformUserRoleAssignment>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Lower_Privileged_Actor_Revokes_The_SuperAdmin_Role_From_Another_User()
    {
        PlatformRole role = SuperAdminRole();
        PlatformUser user = AUser();
        PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(user.Id, role.Id, Now);

        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        SetUpAssignment(user, role, assignment);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(false);
        _superAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, Arg.Any<Guid>(), false))
            .Do(_ => throw new ForbiddenException("Only a Platform Super Admin can manage another Super Admin's account."));

        Func<Task> act = () => CreateHandler().Handle(
            new RevokePlatformRoleFromUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Succeeds_When_Another_Active_Holder_Of_The_SuperAdmin_Role_Remains()
    {
        PlatformRole role = SuperAdminRole();
        PlatformUser user = AUser();
        PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(user.Id, role.Id, Now);

        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        SetUpAssignment(user, role, assignment);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(true);

        await CreateHandler().Handle(new RevokePlatformRoleFromUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None);

        _assignments.Received(1).Remove(assignment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_Not_Consult_Admin_Protection_When_Revoking_A_Non_SuperAdmin_Role()
    {
        PlatformRole role = SupportRole();
        PlatformUser user = AUser();
        PlatformUserRoleAssignment assignment = PlatformUserRoleAssignment.Grant(user.Id, role.Id, Now);

        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        SetUpAssignment(user, role, assignment);
        _superAdminProtection.IsSuperAdmin(role).Returns(false);

        await CreateHandler().Handle(new RevokePlatformRoleFromUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None);

        _superAdminProtection.DidNotReceive().EnsureCanMutateAdminTarget(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>());
        _assignments.Received(1).Remove(assignment);
    }

    [Fact]
    public async Task Throws_NotFound_When_The_User_Does_Not_Hold_The_Role()
    {
        PlatformRole role = SupportRole();
        PlatformUser user = AUser();
        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _assignments.GetForUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);

        Func<Task> act = () => CreateHandler().Handle(
            new RevokePlatformRoleFromUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
