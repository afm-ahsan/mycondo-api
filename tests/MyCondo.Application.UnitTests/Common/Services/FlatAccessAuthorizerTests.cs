using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// FlatOwnership references a Resident, not a portal User directly (Flat Owner Registration) — these
/// tests lock in that a logged-in User's ownership access is still resolved correctly by first finding
/// every Resident bridged to that User, then checking each for an active ownership.
/// </summary>
public class FlatAccessAuthorizerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IOccupancyRegistrationRepository _occupancyRegistrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();

    private FlatAccessAuthorizer CreateAuthorizer() => new(_flatOwnerships, _residents, _occupancyRegistrations, _flats);

    [Fact]
    public async Task HasActiveOwnershipAsync_Is_True_When_A_Bridged_Resident_Has_An_Active_Ownership_For_The_Flat()
    {
        Guid userId = Guid.NewGuid();
        Flat flat = Flat.Create(TenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        resident.LinkToUser(userId);

        _residents.GetByUserIdAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns([resident]);
        _flatOwnerships.ExistsActiveForResidentAndFlatAsync(TenantId, resident.Id.Value, flat.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        bool result = await CreateAuthorizer().HasActiveOwnershipAsync(TenantId, userId, flat.Id.Value, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveOwnershipAsync_Is_False_When_The_User_Has_No_Bridged_Resident()
    {
        Guid userId = Guid.NewGuid();
        _residents.GetByUserIdAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns([]);

        bool result = await CreateAuthorizer().HasActiveOwnershipAsync(TenantId, userId, Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveRelationshipsAsync_Reports_Ownership_For_Every_Flat_The_Bridged_Resident_Owns()
    {
        Guid userId = Guid.NewGuid();
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        Flat flat = Flat.Create(TenantId, building.Id, "A-101", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        resident.LinkToUser(userId);
        FlatOwnership ownership = FlatOwnership.Grant(
            TenantId, resident.Id.Value, flat.Id, DateOnly.FromDateTime(NowUtc.UtcDateTime), NowUtc);

        _residents.GetByUserIdAsync(TenantId, userId, Arg.Any<CancellationToken>()).Returns([resident]);
        _flatOwnerships.GetActiveForResidentAsync(TenantId, resident.Id.Value, Arg.Any<CancellationToken>())
            .Returns([ownership]);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _occupancyRegistrations.GetActiveForFlatAsync(TenantId, flat.Id, Arg.Any<CancellationToken>())
            .Returns((OccupancyRegistration?)null);

        List<FlatRelationship> result = await CreateAuthorizer().GetActiveRelationshipsAsync(TenantId, userId, CancellationToken.None);

        result.Should().ContainSingle(r => r.FlatId == flat.Id.Value && r.Kind == FlatRelationshipKind.Ownership);
    }
}
