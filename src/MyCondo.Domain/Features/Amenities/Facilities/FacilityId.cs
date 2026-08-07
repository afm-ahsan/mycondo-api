namespace MyCondo.Domain.Features.Amenities.Facilities;

public readonly record struct FacilityId(Guid Value)
{
    public static FacilityId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FacilityId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FacilityId(g)
            : throw new FormatException($"Invalid FacilityId: '{s}'");
}
