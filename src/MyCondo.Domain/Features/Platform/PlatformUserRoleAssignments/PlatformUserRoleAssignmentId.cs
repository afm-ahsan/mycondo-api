namespace MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;

public readonly record struct PlatformUserRoleAssignmentId(Guid Value)
{
    public static PlatformUserRoleAssignmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PlatformUserRoleAssignmentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PlatformUserRoleAssignmentId(g)
            : throw new FormatException($"Invalid PlatformUserRoleAssignmentId: '{s}'");
}
