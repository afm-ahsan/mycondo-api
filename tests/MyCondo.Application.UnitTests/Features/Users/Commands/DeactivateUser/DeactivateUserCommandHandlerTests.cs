using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Commands.DeactivateUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.DeactivateUser;

/// <summary>
/// Proves the ADR-036 privileged-target protection wired into <see cref="DeactivateUserCommandHandler"/>:
/// a Tenant Admin cannot self-disable, a lower-privileged actor cannot disable a Tenant Admin, the
/// tenant's last active Tenant Admin can never be disabled, and deactivation always records an
/// <see cref="IdentityAuditLogEntry"/>. Also proves the transaction is opened only when the target
/// actually holds a tenant-wide admin-equivalent role.
/// </summary>
public class DeactivateUserCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly ITenantAdminProtectionService _tenantAdminProtection = Substitute.For<ITenantAdminProtectionService>();
    private readonly IIdentityAuditLogRepository _identityAuditLog = Substitute.For<IIdentityAuditLogRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeactivateUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private DeactivateUserCommandHandler CreateHandler() => new(
        _users, _unitOfWork, _currentUser, _tenantAdminProtection, _identityAuditLog, _clock,
        Substitute.For<ILogger<DeactivateUserCommandHandler>>());

    private static User RegisterUser(Guid tenantId) =>
        User.Register(tenantId, "member@example.com", "hash", "Member", null, NowUtc);

    [Fact]
    public async Task Deactivates_A_Non_Admin_User_Without_Consulting_Protection_Invariants()
    {
        User user = RegisterUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(TenantId, user.Id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateHandler().Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None);

        user.Status.Should().Be(UserStatus.Inactive);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _tenantAdminProtection.DidNotReceive().EnsureNotLastActiveAdminAsync(
            Arg.Any<Guid>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        _identityAuditLog.Received(1).Add(Arg.Is<IdentityAuditLogEntry>(e =>
            e.TenantId == TenantId && e.Action == "User.Deactivate" && e.TargetId == user.Id.Value.ToString()));
    }

    [Fact]
    public async Task Throws_Forbidden_When_Actor_Attempts_To_Self_Disable_A_Tenant_Admin_Target()
    {
        Guid actorId = Guid.NewGuid();
        User user = RegisterUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.UserId.Returns(actorId);
        _tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(TenantId, user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _tenantAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, actorId, Arg.Any<bool>()))
            .Do(_ => throw new ForbiddenException("You cannot perform this action on your own account."));

        Func<Task> act = () => CreateHandler().Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        user.Status.Should().Be(UserStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Lower_Privileged_Actor_Targets_A_Tenant_Admin()
    {
        User user = RegisterUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(false);
        _tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(TenantId, user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _tenantAdminProtection
            .When(p => p.EnsureCanMutateAdminTarget(user.Id.Value, Arg.Any<Guid>(), false))
            .Do(_ => throw new ForbiddenException("Only a Tenant Admin can manage another Tenant Admin's account."));

        Func<Task> act = () => CreateHandler().Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Target_Is_The_Tenants_Last_Active_Admin()
    {
        User user = RegisterUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(true);
        _tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(TenantId, user.Id, Arg.Any<CancellationToken>()).Returns(true);
        _tenantAdminProtection
            .EnsureNotLastActiveAdminAsync(TenantId, user.Id, Arg.Any<CancellationToken>())
            .Returns(_ => throw new ConflictException("Cannot remove this account — it is the last active holder."));

        Func<Task> act = () => CreateHandler().Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
        user.Status.Should().Be(UserStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Opens_A_Transaction_And_Commits_When_Deactivating_A_Tenant_Admin_With_Another_Holder_Remaining()
    {
        User user = RegisterUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.HasPermission("user.manageTenantAdmins").Returns(true);
        _tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(TenantId, user.Id, Arg.Any<CancellationToken>()).Returns(true);
        IUnitOfWorkTransaction transaction = Substitute.For<IUnitOfWorkTransaction>();
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);

        await CreateHandler().Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None);

        user.Status.Should().Be(UserStatus.Inactive);
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Belongs_To_A_Different_Tenant()
    {
        User user = RegisterUser(OtherTenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => CreateHandler().Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Does_Not_Exist()
    {
        Guid userId = Guid.NewGuid();
        _users.GetByIdAsync(new UserId(userId), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => CreateHandler().Handle(new DeactivateUserCommand(userId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(new DeactivateUserCommand(Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
