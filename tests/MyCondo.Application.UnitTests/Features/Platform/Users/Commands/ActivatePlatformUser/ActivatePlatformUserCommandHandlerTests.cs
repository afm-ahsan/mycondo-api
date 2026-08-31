using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.Commands.ActivatePlatformUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Users.Commands.ActivatePlatformUser;

public class ActivatePlatformUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPlatformUserRepository _platformUsers = Substitute.For<IPlatformUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentPlatformUserProvider _currentUser = Substitute.For<ICurrentPlatformUserProvider>();
    private readonly IPlatformSuperAdminProtectionService _superAdminProtection = Substitute.For<IPlatformSuperAdminProtectionService>();
    private readonly IPlatformAuditLogRepository _platformAuditLog = Substitute.For<IPlatformAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ActivatePlatformUserCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(Now);
    }

    private ActivatePlatformUserCommandHandler CreateHandler() => new(
        _platformUsers, _unitOfWork, _currentUser, _superAdminProtection, _platformAuditLog, _clock,
        Substitute.For<ILogger<ActivatePlatformUserCommandHandler>>());

    private static PlatformUser DisabledSuperAdmin()
    {
        PlatformUser user = PlatformUser.Create("admin@mycondo.internal", "hash", "Admin", Now);
        user.Deactivate(Now);
        return user;
    }

    [Fact]
    public async Task Allows_A_Super_Admin_To_Self_Reactivate_Without_ManageSuperAdmins()
    {
        Guid actorId = Guid.NewGuid();
        PlatformUser user = DisabledSuperAdmin();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(actorId);
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .When(p => p.EnsureCanEditAdminTarget(user.Id.Value, actorId, Arg.Any<bool>()))
            .Do(_ => { });

        await CreateHandler().Handle(new ActivatePlatformUserCommand(user.Id.Value), CancellationToken.None);

        user.Status.Should().Be(PlatformUserStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Lower_Privileged_Actor_Reactivates_A_Super_Admin()
    {
        PlatformUser user = DisabledSuperAdmin();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .When(p => p.EnsureCanEditAdminTarget(user.Id.Value, Arg.Any<Guid>(), false))
            .Do(_ => throw new ForbiddenException("Only a Platform Super Admin can manage another Super Admin's account."));

        Func<Task> act = () => CreateHandler().Handle(new ActivatePlatformUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        user.Status.Should().Be(PlatformUserStatus.Disabled);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Platform_User_Does_Not_Exist()
    {
        Guid platformUserId = Guid.NewGuid();
        _platformUsers.GetByIdAsync(new PlatformUserId(platformUserId), Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);

        Func<Task> act = () => CreateHandler().Handle(new ActivatePlatformUserCommand(platformUserId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
