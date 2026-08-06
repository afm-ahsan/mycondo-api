namespace MyCondo.Domain.Features.Utilities.Meters;

public readonly record struct MeterId(Guid Value)
{
    public static MeterId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static MeterId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new MeterId(g)
            : throw new FormatException($"Invalid MeterId: '{s}'");
}
