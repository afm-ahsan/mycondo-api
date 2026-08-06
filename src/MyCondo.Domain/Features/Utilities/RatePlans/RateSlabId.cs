namespace MyCondo.Domain.Features.Utilities.RatePlans;

public readonly record struct RateSlabId(Guid Value)
{
    public static RateSlabId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static RateSlabId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new RateSlabId(g)
            : throw new FormatException($"Invalid RateSlabId: '{s}'");
}
