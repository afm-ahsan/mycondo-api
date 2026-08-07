namespace MyCondo.Domain.Features.Operations.GeneratorSessions;

public readonly record struct GeneratorSessionId(Guid Value)
{
    public static GeneratorSessionId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GeneratorSessionId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GeneratorSessionId(g)
            : throw new FormatException($"Invalid GeneratorSessionId: '{s}'");
}
