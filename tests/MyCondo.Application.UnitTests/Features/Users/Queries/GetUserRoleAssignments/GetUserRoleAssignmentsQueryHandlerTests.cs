using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Users.Queries.GetUserRoleAssignments;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Users.Queries.GetUserRoleAssignments;

public class GetUserRoleAssignmentsQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IRoleAssignmentRepository _roleAssignments = Substitute.For<IRoleAssignmentRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetUserRoleAssignmentsQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetUserRoleAssignmentsQueryHandler CreateHandler() =>
        new(_users, _roles, _roleAssignments, _currentUser);

    [Fact]
    public async Task Returns_The_Users_Role_Assignments_With_Role_Details_Joined_In()
    {
        User user = User.Register(TenantId, "member@example.com", "hash", "Member", null, NowUtc);
        Role role = Role.CreateCustom(TenantId, "Building Manager", "Manages a building", NowUtc, "building.manager");
        Guid buildingId = Guid.NewGuid();
        RoleAssignment assignment = RoleAssignment.Grant(TenantId, user.Id, role.Id, buildingId, NowUtc);

        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _roleAssignments.GetForUserAsync(TenantId, user.Id, Arg.Any<CancellationToken>())
            .Returns([assignment]);
        _roles.GetAllForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([role]);

        List<UserRoleAssignmentDto> result = await CreateHandler()
            .Handle(new GetUserRoleAssignmentsQuery(user.Id.Value), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].RoleId.Should().Be(role.Id.Value);
        result[0].RoleName.Should().Be("Building Manager");
        result[0].BuildingId.Should().Be(buildingId);
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Belongs_To_A_Different_Tenant()
    {
        User user = User.Register(OtherTenantId, "member@example.com", "hash", "Member", null, NowUtc);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new GetUserRoleAssignmentsQuery(user.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
