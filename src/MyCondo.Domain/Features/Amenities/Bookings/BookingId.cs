namespace MyCondo.Domain.Features.Amenities.Bookings;

public readonly record struct BookingId(Guid Value)
{
    public static BookingId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static BookingId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new BookingId(g)
            : throw new FormatException($"Invalid BookingId: '{s}'");
}
