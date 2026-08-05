namespace MyCondo.Domain.Features.Payroll.StaffMembers;

public readonly record struct StaffMemberId(Guid Value)
{
    public static StaffMemberId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static StaffMemberId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new StaffMemberId(g)
            : throw new FormatException($"Invalid StaffMemberId: '{s}'");
}
