using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Commands.CreateUser;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateUserCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(callInfo => $"hashed:{callInfo.Arg<string>()}");
    }

    private CreateUserCommandHandler CreateHandler() => new(
        _users, _unitOfWork, _currentUser, _passwordHasher, _clock,
        Substitute.For<ILogger<CreateUserCommandHandler>>());

    [Fact]
    public async Task Creates_User_In_Callers_Tenant_Never_A_Client_Supplied_Tenant()
    {
        _users.EmailExistsAsync(TenantId, "new.user@example.com", Arg.Any<CancellationToken>()).Returns(false);

        CreateUserCommand command = new("New User", "New.User@example.com", "+8801000000000", "Str0ngPassw0rd!");

        CreateUserResult result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TemporaryPassword.Should().BeNull("an explicit initial password was supplied");
        _users.Received(1).Add(Arg.Is<User>(u => u.TenantId == TenantId && u.Email == "new.user@example.com"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generates_A_Temporary_Password_When_None_Supplied()
    {
        _users.EmailExistsAsync(TenantId, "temp.user@example.com", Arg.Any<CancellationToken>()).Returns(false);

        CreateUserCommand command = new("Temp User", "temp.user@example.com", null, null);

        CreateUserResult result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TemporaryPassword.Should().NotBeNullOrEmpty();
        result.TemporaryPassword!.Length.Should().BeGreaterThanOrEqualTo(12);
        result.TemporaryPassword.Should().MatchRegex("[A-Z]").And.MatchRegex("[a-z]").And.MatchRegex(@"\d");
    }

    [Fact]
    public async Task Throws_Conflict_When_Email_Already_Exists_In_Tenant()
    {
        _users.EmailExistsAsync(TenantId, "taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        CreateUserCommand command = new("Someone", "taken@example.com", null, null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _users.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        CreateUserCommand command = new("Someone", "someone@example.com", null, null);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
