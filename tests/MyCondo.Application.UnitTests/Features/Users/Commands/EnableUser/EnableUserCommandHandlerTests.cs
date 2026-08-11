using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Commands.EnableUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.EnableUser;

public class EnableUserCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public EnableUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private EnableUserCommandHandler CreateHandler() => new(
        _users, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<EnableUserCommandHandler>>());

    private static User RegisterDeactivatedUser(Guid tenantId)
    {
        User user = User.Register(tenantId, "member@example.com", "hash", "Member", null, NowUtc);
        user.Deactivate(NowUtc);
        return user;
    }

    [Fact]
    public async Task Reactivates_A_Deactivated_User_In_Callers_Tenant()
    {
        User user = RegisterDeactivatedUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        EnableUserCommand command = new(user.Id.Value);

        await CreateHandler().Handle(command, CancellationToken.None);

        user.Status.Should().Be(UserStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Belongs_To_A_Different_Tenant()
    {
        User user = RegisterDeactivatedUser(OtherTenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        EnableUserCommand command = new(user.Id.Value);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
