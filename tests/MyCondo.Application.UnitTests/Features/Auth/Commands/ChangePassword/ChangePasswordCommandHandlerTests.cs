using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.Commands.ChangePassword;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Covers the refresh-token revocation added so a password change signs out every other session
/// (previously only the password hash was updated; outstanding refresh tokens stayed valid).
/// </summary>
public class ChangePasswordCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IRequestIpAccessor _ipAccessor = Substitute.For<IRequestIpAccessor>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ChangePasswordCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _ipAccessor.IpAddress.Returns("127.0.0.1");
    }

    private ChangePasswordCommandHandler CreateHandler() => new(
        _users, _refreshTokens, _unitOfWork, _passwordHasher, _currentUser, _ipAccessor, _clock,
        Substitute.For<ILogger<ChangePasswordCommandHandler>>());

    [Fact]
    public async Task Revokes_Every_Active_Refresh_Token_On_Success()
    {
        User user = User.Register(Guid.NewGuid(), "jane@example.com", "old-hash", "Jane Doe", null, NowUtc);
        _currentUser.UserId.Returns(user.Id.Value);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Correct-Password", "old-hash").Returns(true);
        _passwordHasher.Hash("New-Password-123").Returns("new-hash");

        RefreshToken tokenA = RefreshToken.Issue(user.TenantId, user.Id, "hash-a", NowUtc.AddDays(7), NowUtc, "1.1.1.1");
        RefreshToken tokenB = RefreshToken.Issue(user.TenantId, user.Id, "hash-b", NowUtc.AddDays(7), NowUtc, "2.2.2.2");
        _refreshTokens.GetActiveByUserIdAsync(user.Id, NowUtc, Arg.Any<CancellationToken>())
            .Returns([tokenA, tokenB]);

        ChangePasswordCommand command = new("Correct-Password", "New-Password-123");

        await CreateHandler().Handle(command, CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        tokenA.IsRevoked.Should().BeTrue();
        tokenB.IsRevoked.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_And_Does_Not_Touch_Refresh_Tokens_When_Current_Password_Is_Wrong()
    {
        User user = User.Register(Guid.NewGuid(), "jane@example.com", "old-hash", "Jane Doe", null, NowUtc);
        _currentUser.UserId.Returns(user.Id.Value);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Wrong-Password", "old-hash").Returns(false);

        ChangePasswordCommand command = new("Wrong-Password", "New-Password-123");

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("Current password is incorrect.");
        await _refreshTokens.DidNotReceive().GetActiveByUserIdAsync(
            Arg.Any<UserId>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
