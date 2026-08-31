using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.Commands.DeactivatePlatformUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Users.Commands.DeactivatePlatformUser;

/// <summary>
/// Platform-scope analogue of DeactivateUserCommandHandlerTests: a Super Admin cannot self-disable, a
/// lower-privileged actor cannot disable a Super Admin, and the platform's last active Super Admin can
/// never be disabled (mycondo-docs ADR-036). Also proves the transaction opens only when the target
/// actually holds the SuperAdmin role.
/// </summary>
public class DeactivatePlatformUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPlatformUserRepository _platformUsers = Substitute.For<IPlatformUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentPlatformUserProvider _currentUser = Substitute.For<ICurrentPlatformUserProvider>();
    private readonly IPlatformSuperAdminProtectionService _superAdminProtection = Substitute.For<IPlatformSuperAdminProtectionService>();
    private readonly IPlatformAuditLogRepository _platformAuditLog = Substitute.For<IPlatformAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeactivatePlatformUserCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(Now);
    }

    private DeactivatePlatformUserCommandHandler CreateHandler() => new(
        _platformUsers, _unitOfWork, _currentUser, _superAdminProtection, _platformAuditLog, _clock,
        Substitute.For<ILogger<DeactivatePlatformUserCommandHandler>>());

    private static PlatformUser AUser() => PlatformUser.Create("operator@mycondo.internal", "hash", "Operator", Now);

    [Fact]
    public async Task Deactivates_A_Non_Admin_User_Without_Consulting_Protection_Invariants()
    {
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().Handle(new DeactivatePlatformUserCommand(user.Id.Value), CancellationToken.None);

        user.Status.Should().Be(PlatformUserStatus.Disabled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _superAdminProtection.DidNotReceive().EnsureNotLastActiveSuperAdminAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Actor_Attempts_To_Self_Disable_A_Super_Admin_Target()
    {
        Guid actorId = Guid.NewGuid();
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(actorId);
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, actorId, Arg.Any<bool>()))
            .Do(_ => throw new ForbiddenException("You cannot perform this action on your own account."));

        Func<Task> act = () => CreateHandler().Handle(new DeactivatePlatformUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        user.Status.Should().Be(PlatformUserStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Lower_Privileged_Actor_Targets_A_Super_Admin()
    {
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, Arg.Any<Guid>(), false))
            .Do(_ => throw new ForbiddenException("Only a Platform Super Admin can manage another Super Admin's account."));

        Func<Task> act = () => CreateHandler().Handle(new DeactivatePlatformUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Target_Is_The_Platforms_Last_Active_Super_Admin()
    {
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .EnsureNotLastActiveSuperAdminAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new ConflictException("Cannot remove this account — it is the last active Platform Super Admin."));

        Func<Task> act = () => CreateHandler().Handle(new DeactivatePlatformUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        user.Status.Should().Be(PlatformUserStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Opens_A_Transaction_And_Commits_When_Deactivating_A_Super_Admin_With_Another_Holder_Remaining()
    {
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        IUnitOfWorkTransaction transaction = Substitute.For<IUnitOfWorkTransaction>();
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);

        await CreateHandler().Handle(new DeactivatePlatformUserCommand(user.Id.Value), CancellationToken.None);

        user.Status.Should().Be(PlatformUserStatus.Disabled);
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Platform_User_Does_Not_Exist()
    {
        Guid platformUserId = Guid.NewGuid();
        _platformUsers.GetByIdAsync(new PlatformUserId(platformUserId), Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);

        Func<Task> act = () => CreateHandler().Handle(new DeactivatePlatformUserCommand(platformUserId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);

        Func<Task> act = () => CreateHandler().Handle(new DeactivatePlatformUserCommand(Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
