namespace MyCondo.Domain.Features.Security.ParcelCustodyEvents;

public readonly record struct ParcelCustodyEventId(Guid Value)
{
    public static ParcelCustodyEventId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ParcelCustodyEventId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ParcelCustodyEventId(g)
            : throw new FormatException($"Invalid ParcelCustodyEventId: '{s}'");
}
