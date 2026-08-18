using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Common.Services;

public sealed class FlatDisplayNameResolver(
    IFlatRepository flats,
    IBuildingRepository buildings
) : IFlatDisplayNameResolver
{
    private const string UnknownFlat = "Unknown flat";

    public async Task<string> ResolveAsync(FlatId flatId, CancellationToken cancellationToken)
    {
        Flat? flat = await flats.GetByIdAsync(flatId, cancellationToken);
        if (flat is null)
        {
            return UnknownFlat;
        }

        Building? building = await buildings.GetByIdAsync(flat.BuildingId, cancellationToken);
        return Format(flat, building);
    }

    public async Task<IReadOnlyDictionary<FlatId, string>> ResolveManyAsync(
        IReadOnlyCollection<FlatId> flatIds, CancellationToken cancellationToken)
    {
        if (flatIds.Count == 0)
        {
            return new Dictionary<FlatId, string>();
        }

        List<Flat> foundFlats = await flats.GetByIdsAsync(flatIds, cancellationToken);
        List<BuildingId> buildingIds = foundFlats.Select(f => f.BuildingId).Distinct().ToList();
        List<Building> foundBuildings = await buildings.GetByIdsAsync(buildingIds, cancellationToken);
        Dictionary<BuildingId, Building> buildingsById = foundBuildings.ToDictionary(b => b.Id);

        Dictionary<FlatId, string> result = flatIds.ToDictionary(id => id, _ => UnknownFlat);
        foreach (Flat flat in foundFlats)
        {
            buildingsById.TryGetValue(flat.BuildingId, out Building? building);
            result[flat.Id] = Format(flat, building);
        }

        return result;
    }

    private static string Format(Flat flat, Building? building) =>
        building is null ? flat.FlatNumber : $"{building.Code} {flat.FlatNumber}";
}
