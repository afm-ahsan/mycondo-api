namespace MyCondo.Domain.Features.Payments.ResidentAccounts;

public readonly record struct ResidentAccountId(Guid Value)
{
    public static ResidentAccountId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ResidentAccountId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ResidentAccountId(g)
            : throw new FormatException($"Invalid ResidentAccountId: '{s}'");
}
