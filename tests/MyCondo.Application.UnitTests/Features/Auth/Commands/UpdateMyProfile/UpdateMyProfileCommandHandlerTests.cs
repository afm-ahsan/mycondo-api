using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.Commands.UpdateMyProfile;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContextResolver _userContextResolver = Substitute.For<IUserContextResolver>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateMyProfileCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private UpdateMyProfileCommandHandler CreateHandler() => new(
        _users, _unitOfWork, _userContextResolver, _currentUser, _clock,
        Substitute.For<ILogger<UpdateMyProfileCommandHandler>>());

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        UpdateMyProfileCommand command = new("Jane Doe", null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Missing()
    {
        Guid userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _users.GetByIdAsync(new UserId(userId), Arg.Any<CancellationToken>()).Returns((User?)null);

        UpdateMyProfileCommand command = new("Jane Doe", null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Updates_Name_And_Phone_For_The_Caller_Only()
    {
        User user = User.Register(Guid.NewGuid(), "jane@example.com", "hash", "Old Name", null, NowUtc);
        _currentUser.UserId.Returns(user.Id.Value);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        UserProfileDto expectedDto = new(
            user.Id.Value, user.TenantId, user.Email, "Jane Doe", "01700000000", NowUtc, null, [], []);
        _userContextResolver.ResolveProfileAsync(user, Arg.Any<CancellationToken>()).Returns(expectedDto);

        UpdateMyProfileCommand command = new("Jane Doe", "01700000000");

        UserProfileDto result = await CreateHandler().Handle(command, CancellationToken.None);

        user.FullName.Should().Be("Jane Doe");
        user.PhoneNumber.Should().Be("+8801700000000");
        result.Should().Be(expectedDto);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
