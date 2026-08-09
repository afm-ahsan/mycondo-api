namespace MyCondo.Domain.Features.Platform.PlatformRoles;

public readonly record struct PlatformRoleId(Guid Value)
{
    public static PlatformRoleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PlatformRoleId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PlatformRoleId(g)
            : throw new FormatException($"Invalid PlatformRoleId: '{s}'");
}
