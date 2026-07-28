namespace MyCondo.Domain.Features.Identity.RoleAssignments;

public readonly record struct RoleAssignmentId(Guid Value)
{
    public static RoleAssignmentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
