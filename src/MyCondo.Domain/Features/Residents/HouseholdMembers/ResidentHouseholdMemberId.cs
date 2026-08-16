namespace MyCondo.Domain.Features.Residents.HouseholdMembers;

public readonly record struct ResidentHouseholdMemberId(Guid Value)
{
    public static ResidentHouseholdMemberId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ResidentHouseholdMemberId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ResidentHouseholdMemberId(g)
            : throw new FormatException($"Invalid ResidentHouseholdMemberId: '{s}'");
}
