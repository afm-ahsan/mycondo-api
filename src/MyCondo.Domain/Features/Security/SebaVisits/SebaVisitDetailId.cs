namespace MyCondo.Domain.Features.Security.SebaVisits;

public readonly record struct SebaVisitDetailId(Guid Value)
{
    public static SebaVisitDetailId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static SebaVisitDetailId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new SebaVisitDetailId(g)
            : throw new FormatException($"Invalid SebaVisitDetailId: '{s}'");
}
