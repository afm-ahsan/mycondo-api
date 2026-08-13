using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForOwner;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForOwner;

public class GetFlatOwnershipsForOwnerQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetFlatOwnershipsForOwnerQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetFlatOwnershipsForOwnerQueryHandler CreateHandler() => new(
        _flatOwnerships, _flats, _buildings, _residents, _currentUser);

    [Fact]
    public async Task Returns_Every_Flat_The_Owner_Owns_Across_Buildings()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        Flat flat = Flat.Create(TenantId, building.Id, "A-101", 1, FlatType.Residential, NowUtc);
        Resident owner = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        FlatOwnership ownership = FlatOwnership.Grant(
            TenantId, owner.Id.Value, flat.Id, DateOnly.FromDateTime(NowUtc.UtcDateTime), NowUtc);

        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        _flatOwnerships.GetAllForResidentAsync(TenantId, owner.Id.Value, Arg.Any<CancellationToken>())
            .Returns([ownership]);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        List<OwnerFlatOwnershipDto> result = await CreateHandler().Handle(
            new GetFlatOwnershipsForOwnerQuery(owner.Id.Value), CancellationToken.None);

        result.Should().ContainSingle(o => o.FlatId == flat.Id.Value && o.BuildingName == "Tower A");
    }

    [Fact]
    public async Task Throws_NotFound_When_Owner_Belongs_To_A_Different_Tenant()
    {
        FlatId flatId = new(Guid.NewGuid());
        Resident owner = Resident.Register(OtherTenantId, flatId, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetFlatOwnershipsForOwnerQuery(owner.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
