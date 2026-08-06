namespace MyCondo.Domain.Features.Utilities.RatePlans;

public readonly record struct RatePlanId(Guid Value)
{
    public static RatePlanId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static RatePlanId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new RatePlanId(g)
            : throw new FormatException($"Invalid RatePlanId: '{s}'");
}
