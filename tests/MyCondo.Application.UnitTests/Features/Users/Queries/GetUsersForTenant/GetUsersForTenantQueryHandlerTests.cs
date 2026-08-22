using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Queries.GetUsersForTenant;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Queries.GetUsersForTenant;

public class GetUsersForTenantQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleAssignmentRepository _roleAssignments = Substitute.For<IRoleAssignmentRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetUsersForTenantQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _roleAssignments
            .GetForUsersAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _roles.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetUsersForTenantQueryHandler CreateHandler() => new(_users, _roleAssignments, _roles, _currentUser);

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
    public async Task Populates_RoleNames_From_The_Users_Assignments()
    {
        User user = User.Register(TenantId, "member@example.com", "hash", "Member", null, NowUtc);
        _users.SearchAsync(TenantId, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<User>([user], 1, 20, 1));

        Role role = Role.CreateCustom(TenantId, "Building Manager", "Manages a building", NowUtc);
        _roles.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([role]);
        _roleAssignments
            .GetForUsersAsync(TenantId, Arg.Any<IReadOnlyCollection<UserId>>(), Arg.Any<CancellationToken>())
            .Returns([RoleAssignment.Grant(TenantId, user.Id, role.Id, null, NowUtc)]);

        GetUsersForTenantQuery query = new(null, null, null);

        PagedResult<UserSummaryDto> result = await CreateHandler().Handle(query, CancellationToken.None);

        result.Items.Single().RoleNames.Should().ContainSingle("Building Manager");
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
