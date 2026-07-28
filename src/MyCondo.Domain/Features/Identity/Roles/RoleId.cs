namespace MyCondo.Domain.Features.Identity.Roles;

public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static RoleId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new RoleId(g)
            : throw new FormatException($"Invalid RoleId: '{s}'");
}
