namespace MyCondo.Domain.Features.Amenities.PoolIncidents;

public readonly record struct PoolIncidentId(Guid Value)
{
    public static PoolIncidentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PoolIncidentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PoolIncidentId(g)
            : throw new FormatException($"Invalid PoolIncidentId: '{s}'");
}
