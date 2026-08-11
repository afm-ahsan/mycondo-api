using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Queries.GetUsersForTenant;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Queries.GetUsersForTenant;

public class GetUsersForTenantQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetUsersForTenantQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetUsersForTenantQueryHandler CreateHandler() => new(_users, _currentUser);

    [Fact]
    public async Task Delegates_Search_To_The_Repository_Scoped_To_The_Callers_Tenant()
    {
        User user = User.Register(TenantId, "member@example.com", "hash", "Member", null, NowUtc);
        Guid roleId = Guid.NewGuid();
        _users.SearchAsync(TenantId, "mem", roleId, true, 2, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<User>([user], 2, 10, 1));

        GetUsersForTenantQuery query = new("mem", roleId, true, 2, 10);

        PagedResult<UserSummaryDto> result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Items.Should().ContainSingle(u => u.UserId == user.Id.Value);
        result.Page.Should().Be(2);
        result.Total.Should().Be(1);
        await _users.Received(1).SearchAsync(TenantId, "mem", roleId, true, 2, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Not_Authenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new GetUsersForTenantQuery(null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
