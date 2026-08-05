namespace MyCondo.Domain.Features.Security.DomesticWorkerAssignments;

public readonly record struct DomesticWorkerAssignmentId(Guid Value)
{
    public static DomesticWorkerAssignmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static DomesticWorkerAssignmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new DomesticWorkerAssignmentId(g)
            : throw new FormatException($"Invalid DomesticWorkerAssignmentId: '{s}'");
}
