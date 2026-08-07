namespace MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

public readonly record struct GeneratorServiceRecordId(Guid Value)
{
    public static GeneratorServiceRecordId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GeneratorServiceRecordId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GeneratorServiceRecordId(g)
            : throw new FormatException($"Invalid GeneratorServiceRecordId: '{s}'");
}
