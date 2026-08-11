using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Queries.GetUserById;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetUserByIdQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetUserByIdQueryHandler CreateHandler() => new(_users, _currentUser);

    [Fact]
    public async Task Returns_The_Users_Detail_When_In_Callers_Tenant()
    {
        User user = User.Register(TenantId, "member@example.com", "hash", "Member", "+8801000000000", NowUtc);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        UserDetailDto result = await CreateHandler().Handle(new GetUserByIdQuery(user.Id.Value), CancellationToken.None);

        result.UserId.Should().Be(user.Id.Value);
        result.Email.Should().Be("member@example.com");
        result.PhoneNumber.Should().Be("+8801000000000");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Belongs_To_A_Different_Tenant()
    {
        User user = User.Register(OtherTenantId, "member@example.com", "hash", "Member", null, NowUtc);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new GetUserByIdQuery(user.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
