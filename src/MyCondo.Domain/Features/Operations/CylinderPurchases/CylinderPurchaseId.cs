namespace MyCondo.Domain.Features.Operations.CylinderPurchases;

public readonly record struct CylinderPurchaseId(Guid Value)
{
    public static CylinderPurchaseId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static CylinderPurchaseId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new CylinderPurchaseId(g)
            : throw new FormatException($"Invalid CylinderPurchaseId: '{s}'");
}
