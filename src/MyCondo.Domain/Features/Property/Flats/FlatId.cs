namespace MyCondo.Domain.Features.Property.Flats;

public readonly record struct FlatId(Guid Value)
{
    public static FlatId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FlatId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FlatId(g)
            : throw new FormatException($"Invalid FlatId: '{s}'");
}
