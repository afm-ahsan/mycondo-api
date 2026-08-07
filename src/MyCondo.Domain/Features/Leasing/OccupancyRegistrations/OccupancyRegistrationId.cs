namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

public readonly record struct OccupancyRegistrationId(Guid Value)
{
    public static OccupancyRegistrationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static OccupancyRegistrationId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new OccupancyRegistrationId(g)
            : throw new FormatException($"Invalid OccupancyRegistrationId: '{s}'");
}
