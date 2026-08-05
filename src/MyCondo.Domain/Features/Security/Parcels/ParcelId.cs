namespace MyCondo.Domain.Features.Security.Parcels;

public readonly record struct ParcelId(Guid Value)
{
    public static ParcelId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ParcelId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ParcelId(g)
            : throw new FormatException($"Invalid ParcelId: '{s}'");
}
