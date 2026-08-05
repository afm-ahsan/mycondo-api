namespace MyCondo.Domain.Features.Property.Gates;

public readonly record struct GateId(Guid Value)
{
    public static GateId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GateId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GateId(g)
            : throw new FormatException($"Invalid GateId: '{s}'");
}
