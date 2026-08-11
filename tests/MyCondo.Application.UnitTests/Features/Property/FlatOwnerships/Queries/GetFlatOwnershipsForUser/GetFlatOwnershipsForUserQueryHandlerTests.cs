using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForUser;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForUser;

public class GetFlatOwnershipsForUserQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetFlatOwnershipsForUserQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetFlatOwnershipsForUserQueryHandler CreateHandler() =>
        new(_flatOwnerships, _flats, _buildings, _users, _currentUser);

    [Fact]
    public async Task Returns_Every_Flat_The_User_Owns_With_Building_Details_Joined_In()
    {
        User owner = User.Register(TenantId, "owner@example.com", "hash", "Owner", null, NowUtc);
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        Flat flat = Flat.Create(TenantId, building.Id, "A-101", 1, FlatType.Residential, NowUtc);
        FlatOwnership ownership = FlatOwnership.Grant(TenantId, owner.Id.Value, flat.Id, new DateOnly(2026, 1, 1), NowUtc);

        _users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        _flatOwnerships.GetAllForUserAsync(TenantId, owner.Id.Value, Arg.Any<CancellationToken>())
            .Returns([ownership]);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        List<UserFlatOwnershipDto> result = await CreateHandler()
            .Handle(new GetFlatOwnershipsForUserQuery(owner.Id.Value), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].FlatNumber.Should().Be("A-101");
        result[0].BuildingName.Should().Be("Tower A");
        result[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Throws_NotFound_When_User_Belongs_To_A_Different_Tenant()
    {
        User owner = User.Register(OtherTenantId, "owner@example.com", "hash", "Owner", null, NowUtc);
        _users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new GetFlatOwnershipsForUserQuery(owner.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
