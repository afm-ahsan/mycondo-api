namespace MyCondo.Domain.Features.Billing.ServiceChargeRules;

public readonly record struct ServiceChargeRuleId(Guid Value)
{
    public static ServiceChargeRuleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ServiceChargeRuleId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ServiceChargeRuleId(g)
            : throw new FormatException($"Invalid ServiceChargeRuleId: '{s}'");
}
