namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;

public readonly record struct OccupancyRegistrationWorkerAssignmentId(Guid Value)
{
    public static OccupancyRegistrationWorkerAssignmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static OccupancyRegistrationWorkerAssignmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new OccupancyRegistrationWorkerAssignmentId(g)
            : throw new FormatException($"Invalid OccupancyRegistrationWorkerAssignmentId: '{s}'");
}
