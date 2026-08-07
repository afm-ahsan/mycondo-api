namespace MyCondo.Domain.Features.Amenities.PoolSessions;

public readonly record struct PoolSessionId(Guid Value)
{
    public static PoolSessionId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PoolSessionId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PoolSessionId(g)
            : throw new FormatException($"Invalid PoolSessionId: '{s}'");
}
