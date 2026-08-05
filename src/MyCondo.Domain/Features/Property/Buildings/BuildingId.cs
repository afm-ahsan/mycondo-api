namespace MyCondo.Domain.Features.Property.Buildings;

public readonly record struct BuildingId(Guid Value)
{
    public static BuildingId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static BuildingId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new BuildingId(g)
            : throw new FormatException($"Invalid BuildingId: '{s}'");
}
