using AwesomeAssertions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Backs the "no raw GUIDs in the UI" rule for flat references (e.g. Parcel recipient flat) — the
/// resolver composes a business-facing display name so callers never need to surface a FlatId.
/// </summary>
public class FlatDisplayNameResolverTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();

    private FlatDisplayNameResolver CreateResolver() => new(_flats, _buildings);

    [Fact]
    public async Task ResolveAsync_Composes_Building_Code_And_Flat_Number()
    {
        Building building = Building.Create(TenantId, "Aisha Tower", "AISHA", null, NowUtc);
        Flat flat = Flat.Create(TenantId, building.Id, "A8", 8, FlatType.Residential, NowUtc);

        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        string result = await CreateResolver().ResolveAsync(flat.Id, CancellationToken.None);

        result.Should().Be("AISHA A8");
    }

    [Fact]
    public async Task ResolveAsync_Falls_Back_To_Unknown_Flat_When_The_Flat_No_Longer_Exists()
    {
        FlatId flatId = FlatId.New();
        _flats.GetByIdAsync(flatId, Arg.Any<CancellationToken>()).Returns((Flat?)null);

        string result = await CreateResolver().ResolveAsync(flatId, CancellationToken.None);

        result.Should().Be("Unknown flat");
    }

    [Fact]
    public async Task ResolveManyAsync_Resolves_Every_Requested_Id_Including_Missing_Ones()
    {
        Building building = Building.Create(TenantId, "Aisha Tower", "AISHA", null, NowUtc);
        Flat flat = Flat.Create(TenantId, building.Id, "A8", 8, FlatType.Residential, NowUtc);
        FlatId missingFlatId = FlatId.New();

        _flats.GetByIdsAsync(Arg.Any<IReadOnlyCollection<FlatId>>(), Arg.Any<CancellationToken>())
            .Returns([flat]);
        _buildings.GetByIdsAsync(Arg.Any<IReadOnlyCollection<BuildingId>>(), Arg.Any<CancellationToken>())
            .Returns([building]);

        IReadOnlyDictionary<FlatId, string> result = await CreateResolver().ResolveManyAsync(
            [flat.Id, missingFlatId], CancellationToken.None);

        result[flat.Id].Should().Be("AISHA A8");
        result[missingFlatId].Should().Be("Unknown flat");
    }
}
