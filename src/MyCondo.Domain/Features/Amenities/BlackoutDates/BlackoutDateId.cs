namespace MyCondo.Domain.Features.Amenities.BlackoutDates;

public readonly record struct BlackoutDateId(Guid Value)
{
    public static BlackoutDateId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static BlackoutDateId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new BlackoutDateId(g)
            : throw new FormatException($"Invalid BlackoutDateId: '{s}'");
}
