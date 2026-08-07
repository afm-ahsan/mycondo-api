namespace MyCondo.Domain.Features.Operations.Generators;

public readonly record struct GeneratorId(Guid Value)
{
    public static GeneratorId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GeneratorId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GeneratorId(g)
            : throw new FormatException($"Invalid GeneratorId: '{s}'");
}
