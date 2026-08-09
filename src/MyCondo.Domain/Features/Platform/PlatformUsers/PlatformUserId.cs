namespace MyCondo.Domain.Features.Platform.PlatformUsers;

public readonly record struct PlatformUserId(Guid Value)
{
    public static PlatformUserId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static PlatformUserId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new PlatformUserId(g)
            : throw new FormatException($"Invalid PlatformUserId: '{s}'");
}
