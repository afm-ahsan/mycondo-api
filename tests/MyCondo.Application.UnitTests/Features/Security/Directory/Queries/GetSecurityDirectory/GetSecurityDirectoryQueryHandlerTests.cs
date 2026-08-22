using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Directory.DTOs;
using MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectory;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Security.Directory.Queries.GetSecurityDirectory;

/// <summary>Proves the list merges active Tenant (OccupancyRegistration) and active Owner
/// (FlatOwnership) rows into one directory, and that search/access-status filtering applies uniformly
/// across both sources.</summary>
public class GetSecurityDirectoryQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly FlatId FlatId = FlatId.New();

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetSecurityDirectoryQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);

        Building building = Building.Create(TenantId, "Tower A", "A", null, Now);
        Flat flat = Flat.Create(TenantId, building.Id, "A-101", 1, FlatType.Residential, Now);
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>()).Returns(flat);
        _buildings.GetByIdAsync(flat.BuildingId, Arg.Any<CancellationToken>()).Returns(building);
    }

    private GetSecurityDirectoryQueryHandler CreateHandler() => new(
        _registrations, _flatOwnerships, _residents, _flats, _buildings, _currentUser);

    private static OccupancyRegistration ActiveTenant(string name, string phone)
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId.New(), ResidentType.Occupant, name, phone, null, "1234567890",
            new DateOnly(1990, 1, 1), "Female", null, null, null, null, null, null, null, null, null, null, null,
            Now);
        registration.Submit(Guid.NewGuid(), Now);
        registration.ApproveByOwner(Guid.NewGuid(), Now);
        registration.VerifyByManagement(Guid.NewGuid(), Now);
        registration.Activate(Now);
        return registration;
    }

    [Fact]
    public async Task Merges_Active_Owner_And_Tenant_Rows()
    {
        OccupancyRegistration tenant = ActiveTenant("Jane Tenant", "01700000000");
        Resident owner = Resident.Register(TenantId, FlatId, "John Owner", "01800000000", null, ResidentType.Owner, Now);
        FlatOwnership ownership = FlatOwnership.Grant(TenantId, owner.Id.Value, FlatId, new DateOnly(2020, 1, 1), Now);

        _registrations.SearchAsync(TenantId, null, OccupancyRegistrationStatus.Active, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OccupancyRegistration>([tenant], 1, 10_000, 1));
        _flatOwnerships.SearchAsync(TenantId, null, FlatOwnershipStatus.Active, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<FlatOwnership>([ownership], 1, 10_000, 1));
        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        PagedResult<SecurityDirectoryEntryDto> result = await CreateHandler().Handle(
            new GetSecurityDirectoryQuery(null, null, null, null, 1, 50), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().Contain(e => e.PrimaryFullName == "Jane Tenant" && e.ResidentType == "Tenant");
        result.Items.Should().Contain(e => e.PrimaryFullName == "John Owner" && e.ResidentType == "Owner");
    }

    [Fact]
    public async Task Search_Matches_Phone_Number()
    {
        OccupancyRegistration tenant = ActiveTenant("Jane Tenant", "01711112222");
        Resident owner = Resident.Register(TenantId, FlatId, "John Owner", "01899998888", null, ResidentType.Owner, Now);
        FlatOwnership ownership = FlatOwnership.Grant(TenantId, owner.Id.Value, FlatId, new DateOnly(2020, 1, 1), Now);

        _registrations.SearchAsync(TenantId, null, OccupancyRegistrationStatus.Active, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OccupancyRegistration>([tenant], 1, 10_000, 1));
        _flatOwnerships.SearchAsync(TenantId, null, FlatOwnershipStatus.Active, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<FlatOwnership>([ownership], 1, 10_000, 1));
        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        PagedResult<SecurityDirectoryEntryDto> result = await CreateHandler().Handle(
            new GetSecurityDirectoryQuery("01711112222", null, null, null, 1, 50), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().PrimaryFullName.Should().Be("Jane Tenant");
    }

    [Fact]
    public async Task AccessStatus_Filter_Excludes_Revoked_Owners()
    {
        Resident owner = Resident.Register(TenantId, FlatId, "John Owner", null, null, ResidentType.Owner, Now);
        FlatOwnership ownership = FlatOwnership.Grant(TenantId, owner.Id.Value, FlatId, new DateOnly(2020, 1, 1), Now);
        ownership.End(new DateOnly(2024, 1, 1), Now);

        _registrations.SearchAsync(TenantId, null, OccupancyRegistrationStatus.Active, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OccupancyRegistration>([], 1, 10_000, 0));
        // Owner's ownership already ended, so it won't come back from a status=Active repo search in
        // practice; this test simulates the defensive in-handler filter directly by still returning it
        // (e.g. a status transition mid-request) to prove the AccessStatus query filter also protects here.
        _flatOwnerships.SearchAsync(TenantId, null, FlatOwnershipStatus.Active, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<FlatOwnership>([ownership], 1, 10_000, 1));
        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        PagedResult<SecurityDirectoryEntryDto> result = await CreateHandler().Handle(
            new GetSecurityDirectoryQuery(null, null, null, "Authorized", 1, 50), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}
