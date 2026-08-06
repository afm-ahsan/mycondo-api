namespace MyCondo.Domain.Features.Utilities.MeterAssignments;

public readonly record struct MeterAssignmentId(Guid Value)
{
    public static MeterAssignmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static MeterAssignmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new MeterAssignmentId(g)
            : throw new FormatException($"Invalid MeterAssignmentId: '{s}'");
}
