namespace MyCondo.Domain.Features.Property.FlatOwnerships;

public readonly record struct FlatOwnershipId(Guid Value)
{
    public static FlatOwnershipId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FlatOwnershipId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FlatOwnershipId(g)
            : throw new FormatException($"Invalid FlatOwnershipId: '{s}'");
}
