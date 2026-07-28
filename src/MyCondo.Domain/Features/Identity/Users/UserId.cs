namespace MyCondo.Domain.Features.Identity.Users;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static UserId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new UserId(g)
            : throw new FormatException($"Invalid UserId: '{s}'");
}
