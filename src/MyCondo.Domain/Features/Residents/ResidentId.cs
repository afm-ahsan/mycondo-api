namespace MyCondo.Domain.Features.Residents;

public readonly record struct ResidentId(Guid Value)
{
    public static ResidentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ResidentId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ResidentId(g)
            : throw new FormatException($"Invalid ResidentId: '{s}'");
}
