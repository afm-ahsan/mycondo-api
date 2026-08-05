namespace MyCondo.Domain.Features.Security.Vehicles;

public readonly record struct VehicleId(Guid Value)
{
    public static VehicleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static VehicleId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new VehicleId(g)
            : throw new FormatException($"Invalid VehicleId: '{s}'");
}
