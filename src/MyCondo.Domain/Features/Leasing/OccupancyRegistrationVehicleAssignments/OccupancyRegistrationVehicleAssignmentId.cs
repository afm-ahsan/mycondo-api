namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;

public readonly record struct OccupancyRegistrationVehicleAssignmentId(Guid Value)
{
    public static OccupancyRegistrationVehicleAssignmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static OccupancyRegistrationVehicleAssignmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new OccupancyRegistrationVehicleAssignmentId(g)
            : throw new FormatException($"Invalid OccupancyRegistrationVehicleAssignmentId: '{s}'");
}
