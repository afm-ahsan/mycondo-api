namespace MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;

public readonly record struct GeneratorBreakdownRecordId(Guid Value)
{
    public static GeneratorBreakdownRecordId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GeneratorBreakdownRecordId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GeneratorBreakdownRecordId(g)
            : throw new FormatException($"Invalid GeneratorBreakdownRecordId: '{s}'");
}
