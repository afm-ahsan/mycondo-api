namespace MyCondo.Domain.Features.Leasing.HouseholdMembers;

public readonly record struct HouseholdMemberId(Guid Value)
{
    public static HouseholdMemberId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static HouseholdMemberId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new HouseholdMemberId(g)
            : throw new FormatException($"Invalid HouseholdMemberId: '{s}'");
}
