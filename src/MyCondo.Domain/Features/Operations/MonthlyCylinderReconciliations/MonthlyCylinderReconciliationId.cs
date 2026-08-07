namespace MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

public readonly record struct MonthlyCylinderReconciliationId(Guid Value)
{
    public static MonthlyCylinderReconciliationId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static MonthlyCylinderReconciliationId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new MonthlyCylinderReconciliationId(g)
            : throw new FormatException($"Invalid MonthlyCylinderReconciliationId: '{s}'");
}
