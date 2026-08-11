using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Commands.UpdateUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private UpdateUserCommandHandler CreateHandler() => new(
        _users, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<UpdateUserCommandHandler>>());

    private static User RegisterUser(Guid tenantId) => User.Register(
        tenantId, "member@example.com", "hash", "Original Name", null, NowUtc);

    [Fact]
    public async Task Updates_FullName_And_PhoneNumber_For_A_User_In_Callers_Tenant()
    {
        User user = RegisterUser(TenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        UpdateUserCommand command = new(user.Id.Value, "Updated Name", "+8801700000000");

        await CreateHandler().Handle(command, CancellationToken.None);

        user.FullName.Should().Be("Updated Name");
        user.PhoneNumber.Should().Be("+8801700000000");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Belongs_To_A_Different_Tenant()
    {
        User user = RegisterUser(OtherTenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        UpdateUserCommand command = new(user.Id.Value, "Updated Name", null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Does_Not_Exist()
    {
        Guid userId = Guid.NewGuid();
        _users.GetByIdAsync(new UserId(userId), Arg.Any<CancellationToken>()).Returns((User?)null);

        UpdateUserCommand command = new(userId, "Updated Name", null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
