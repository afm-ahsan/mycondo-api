namespace MyCondo.Domain.Features.Security.ServiceProviderAssignments;

public readonly record struct ServiceProviderAssignmentId(Guid Value)
{
    public static ServiceProviderAssignmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ServiceProviderAssignmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ServiceProviderAssignmentId(g)
            : throw new FormatException($"Invalid ServiceProviderAssignmentId: '{s}'");
}
