namespace MyCondo.Domain.Features.Security.Guests;

public readonly record struct GuestProfileId(Guid Value)
{
    public static GuestProfileId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GuestProfileId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GuestProfileId(g)
            : throw new FormatException($"Invalid GuestProfileId: '{s}'");
}
