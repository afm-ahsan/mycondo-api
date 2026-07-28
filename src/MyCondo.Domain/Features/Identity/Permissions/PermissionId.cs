namespace MyCondo.Domain.Features.Identity.Permissions;

public readonly record struct PermissionId(Guid Value)
{
    public static PermissionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
