using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.Commands.CreatePlatformUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Users.Commands.CreatePlatformUser;

/// <summary>
/// Proves the ADR-035 grant-time gate in <see cref="CreatePlatformUserCommandHandler"/>: creating a
/// Platform user with an initial SuperAdmin role requires <c>platform.user.manageSuperAdmins</c>,
/// mirroring the tenant <c>AssignRoleToUserCommandHandler</c>'s self-promotion-not-exempt behavior.
/// </summary>
public class CreatePlatformUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPlatformUserRepository _platformUsers = Substitute.For<IPlatformUserRepository>();
    private readonly IPlatformRoleRepository _platformRoles = Substitute.For<IPlatformRoleRepository>();
    private readonly IPlatformUserRoleAssignmentRepository _assignments = Substitute.For<IPlatformUserRoleAssignmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentPlatformUserProvider _currentUser = Substitute.For<ICurrentPlatformUserProvider>();
    private readonly IPlatformSuperAdminProtectionService _superAdminProtection = Substitute.For<IPlatformSuperAdminProtectionService>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPlatformAuditLogRepository _platformAuditLog = Substitute.For<IPlatformAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreatePlatformUserCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(Now);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(callInfo => $"hashed:{callInfo.Arg<string>()}");
    }

    private CreatePlatformUserCommandHandler CreateHandler() => new(
        _platformUsers, _platformRoles, _assignments, _unitOfWork, _currentUser, _superAdminProtection,
        _passwordHasher, _platformAuditLog, _clock, Substitute.For<ILogger<CreatePlatformUserCommandHandler>>());

    private static PlatformRole SuperAdminRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "SuperAdmin", "Full access", Now);

    private static PlatformRole SupportRole() =>
        PlatformRole.CreateSystem(PlatformRoleId.New(), "Support", "Limited access", Now);

    [Fact]
    public async Task Creates_A_Platform_User_With_No_Initial_Role()
    {
        _platformUsers.GetByEmailAsync("new.operator@mycondo.internal", Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);

        CreatePlatformUserCommand command = new("New Operator", "New.Operator@mycondo.internal", "Str0ngPassw0rd!", true, null);

        CreatePlatformUserResult result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PlatformUserId.Should().NotBeEmpty();
        _platformUsers.Received(1).Add(Arg.Is<PlatformUser>(u => u.Email == "new.operator@mycondo.internal"));
        _assignments.DidNotReceive().Add(Arg.Any<PlatformUserRoleAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Granting_The_SuperAdmin_Role_Without_ManageSuperAdmins()
    {
        PlatformRole superAdmin = SuperAdminRole();
        _platformUsers.GetByEmailAsync("attempted@mycondo.internal", Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns(superAdmin);
        _superAdminProtection.IsSuperAdmin(superAdmin).Returns(true);
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(false);

        CreatePlatformUserCommand command = new(
            "Attempted", "attempted@mycondo.internal", "Str0ngPassw0rd!", true, "SuperAdmin");

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        _platformUsers.DidNotReceive().Add(Arg.Any<PlatformUser>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeds_Granting_The_SuperAdmin_Role_When_Actor_Has_ManageSuperAdmins()
    {
        PlatformRole superAdmin = SuperAdminRole();
        _platformUsers.GetByEmailAsync("new.admin@mycondo.internal", Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);
        _platformRoles.GetByNameAsync("SuperAdmin", Arg.Any<CancellationToken>()).Returns(superAdmin);
        _superAdminProtection.IsSuperAdmin(superAdmin).Returns(true);
        _currentUser.HasPermission("platform.user.manageSuperAdmins").Returns(true);

        CreatePlatformUserCommand command = new(
            "New Admin", "new.admin@mycondo.internal", "Str0ngPassw0rd!", true, "SuperAdmin");

        await CreateHandler().Handle(command, CancellationToken.None);

        _assignments.Received(1).Add(Arg.Any<PlatformUserRoleAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Succeeds_Granting_A_Non_SuperAdmin_Role_Without_ManageSuperAdmins()
    {
        PlatformRole support = SupportRole();
        _platformUsers.GetByEmailAsync("support@mycondo.internal", Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);
        _platformRoles.GetByNameAsync("Support", Arg.Any<CancellationToken>()).Returns(support);
        _superAdminProtection.IsSuperAdmin(support).Returns(false);

        CreatePlatformUserCommand command = new(
            "Support Agent", "support@mycondo.internal", "Str0ngPassw0rd!", true, "Support");

        await CreateHandler().Handle(command, CancellationToken.None);

        _assignments.Received(1).Add(Arg.Any<PlatformUserRoleAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Email_Already_Exists()
    {
        _platformUsers.GetByEmailAsync("taken@mycondo.internal", Arg.Any<CancellationToken>())
            .Returns(PlatformUser.Create("taken@mycondo.internal", "hash", "Existing", Now));

        CreatePlatformUserCommand command = new("Someone", "taken@mycondo.internal", "Str0ngPassw0rd!", true, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);

        CreatePlatformUserCommand command = new("Someone", "someone@mycondo.internal", "Str0ngPassw0rd!", true, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
