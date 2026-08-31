using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Users.Commands.UpdatePlatformUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Users.Commands.UpdatePlatformUser;

/// <summary>
/// Platform-scope analogue of UpdateUserCommandHandlerTests: a Super Admin may always edit its own
/// profile; a lower-privileged actor may never edit another Super Admin's profile (mycondo-docs ADR-036).
/// </summary>
public class UpdatePlatformUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IPlatformUserRepository _platformUsers = Substitute.For<IPlatformUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentPlatformUserProvider _currentUser = Substitute.For<ICurrentPlatformUserProvider>();
    private readonly IPlatformSuperAdminProtectionService _superAdminProtection = Substitute.For<IPlatformSuperAdminProtectionService>();
    private readonly IPlatformAuditLogRepository _platformAuditLog = Substitute.For<IPlatformAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdatePlatformUserCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _clock.UtcNow.Returns(Now);
    }

    private UpdatePlatformUserCommandHandler CreateHandler() => new(
        _platformUsers, _unitOfWork, _currentUser, _superAdminProtection, _platformAuditLog, _clock,
        Substitute.For<ILogger<UpdatePlatformUserCommandHandler>>());

    private static PlatformUser AUser() => PlatformUser.Create("operator@mycondo.internal", "hash", "Original Name", Now);

    [Fact]
    public async Task Updates_A_Non_Admin_Users_Display_Name()
    {
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().Handle(new UpdatePlatformUserCommand(user.Id.Value, "Updated Name"), CancellationToken.None);

        user.DisplayName.Should().Be("Updated Name");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_A_Super_Admin_To_Edit_Its_Own_Profile_Without_ManageSuperAdmins()
    {
        Guid actorId = Guid.NewGuid();
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(actorId);
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .When(p => p.EnsureCanEditAdminTarget(user.Id.Value, actorId, Arg.Any<bool>()))
            .Do(_ => { });

        await CreateHandler().Handle(new UpdatePlatformUserCommand(user.Id.Value, "Self Updated"), CancellationToken.None);

        user.DisplayName.Should().Be("Self Updated");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Lower_Privileged_Actor_Edits_A_Super_Admin()
    {
        PlatformUser user = AUser();
        _platformUsers.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.PlatformUserId.Returns(Guid.NewGuid());
        _superAdminProtection.TargetIsSuperAdminAsync(user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _superAdminProtection
            .When(p => p.EnsureCanEditAdminTarget(user.Id.Value, Arg.Any<Guid>(), false))
            .Do(_ => throw new ForbiddenException("Only a Platform Super Admin can manage another Super Admin's account."));

        Func<Task> act = () => CreateHandler().Handle(
            new UpdatePlatformUserCommand(user.Id.Value, "Attempted Update"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        user.DisplayName.Should().Be("Original Name");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Platform_User_Does_Not_Exist()
    {
        Guid platformUserId = Guid.NewGuid();
        _platformUsers.GetByIdAsync(new PlatformUserId(platformUserId), Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new UpdatePlatformUserCommand(platformUserId, "Name"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);

        Func<Task> act = () => CreateHandler().Handle(
            new UpdatePlatformUserCommand(Guid.NewGuid(), "Name"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
