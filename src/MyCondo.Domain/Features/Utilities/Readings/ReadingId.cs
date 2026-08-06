namespace MyCondo.Domain.Features.Utilities.Readings;

public readonly record struct ReadingId(Guid Value)
{
    public static ReadingId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ReadingId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ReadingId(g)
            : throw new FormatException($"Invalid ReadingId: '{s}'");
}
