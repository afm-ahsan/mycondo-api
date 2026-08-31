using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.Commands.AssignPlatformRoleToUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Users.Commands.AssignPlatformRoleToUser;

/// <summary>
/// Platform-scope analogue of AssignRoleToUserCommandHandlerTests: granting the SuperAdmin role —
/// including to oneself — requires <c>platform.user.manageSuperAdmins</c> (mycondo-docs ADR-036).
/// </summary>
public class AssignPlatformRoleToUserCommandHandlerTests
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

    public AssignPlatformRoleToUserCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(Now);
    }

    private AssignPlatformRoleToUserCommandHandler CreateHandler() => new(
        _platformRoles, _platformUsers, _assignments, _unitOfWork, _currentUser, _superAdminProtection,
        _platformAuditLog, _clock, Substitute.For<ILogger<AssignPlatformRoleToUserCommandHandler>>());

    private static PlatformRole SuperAdminRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "SuperAdmin", "Full access", Now);

    private static PlatformRole SupportRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "Support", "Limited access", Now);

    private static PlatformUser AUser() => PlatformUser.Create("operator@mycondo.internal", "hash", "Operator", Now);

    [Fact]
    public async Task Throws_Forbidden_When_Granting_The_SuperAdmin_Role_Without_ManageSuperAdmins()
    {
        PlatformRole role = SuperAdminRole();
        PlatformUser user = AUser();
        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(false);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignPlatformRoleToUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        _assignments.DidNotReceive().Add(Arg.Any<PlatformUserRoleAssignment>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Actor_Attempts_To_Self_Grant_The_SuperAdmin_Role()
    {
        PlatformRole role = SuperAdminRole();
        Guid actorId = Guid.NewGuid();
        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(false);
        _currentUser.PlatformUserId.Returns(actorId);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignPlatformRoleToUserCommand(role.Id.Value, actorId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Succeeds_Granting_The_SuperAdmin_Role_When_Actor_Has_ManageSuperAdmins()
    {
        PlatformRole role = SuperAdminRole();
        PlatformUser user = AUser();
        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _superAdminProtection.IsSuperAdmin(role).Returns(true);
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(true);
        _assignments.ExistsAsync(user.Id, role.Id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().Handle(new AssignPlatformRoleToUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None);

        _assignments.Received(1).Add(Arg.Any<PlatformUserRoleAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeds_Granting_A_Non_SuperAdmin_Role_Without_ManageSuperAdmins()
    {
        PlatformRole role = SupportRole();
        PlatformUser user = AUser();
        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _superAdminProtection.IsSuperAdmin(role).Returns(false);
        _assignments.ExistsAsync(user.Id, role.Id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().Handle(new AssignPlatformRoleToUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None);

        _assignments.Received(1).Add(Arg.Any<PlatformUserRoleAssignment>());
    }

    [Fact]
    public async Task Throws_Conflict_When_The_User_Already_Holds_The_Role()
    {
        PlatformRole role = SupportRole();
        PlatformUser user = AUser();
        _platformRoles.GetByIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(role);
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _superAdminProtection.IsSuperAdmin(role).Returns(false);
        _assignments.ExistsAsync(user.Id, role.Id, Arg.Any<CancellationToken>()).Returns(true);

        Func<Task> act = () => CreateHandler().Handle(
            new AssignPlatformRoleToUserCommand(role.Id.Value, user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        _assignments.DidNotReceive().Add(Arg.Any<PlatformUserRoleAssignment>());
    }
}
