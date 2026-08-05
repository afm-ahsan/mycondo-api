namespace MyCondo.Domain.Features.Security.AccessSessions;

public readonly record struct AccessSessionId(Guid Value)
{
    public static AccessSessionId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static AccessSessionId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new AccessSessionId(g)
            : throw new FormatException($"Invalid AccessSessionId: '{s}'");
}
