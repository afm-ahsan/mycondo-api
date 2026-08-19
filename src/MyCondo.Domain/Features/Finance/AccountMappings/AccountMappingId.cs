namespace MyCondo.Domain.Features.Finance.AccountMappings;

public readonly record struct AccountMappingId(Guid Value)
{
    public static AccountMappingId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static AccountMappingId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new AccountMappingId(g)
            : throw new FormatException($"Invalid AccountMappingId: '{s}'");
}
