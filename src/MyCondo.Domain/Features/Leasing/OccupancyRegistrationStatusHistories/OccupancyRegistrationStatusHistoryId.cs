namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

public readonly record struct OccupancyRegistrationStatusHistoryId(Guid Value)
{
    public static OccupancyRegistrationStatusHistoryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static OccupancyRegistrationStatusHistoryId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new OccupancyRegistrationStatusHistoryId(g)
            : throw new FormatException($"Invalid OccupancyRegistrationStatusHistoryId: '{s}'");
}
