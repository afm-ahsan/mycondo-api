namespace MyCondo.Domain.Features.Operations.CylinderStockMovements;

public readonly record struct CylinderStockMovementId(Guid Value)
{
    public static CylinderStockMovementId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static CylinderStockMovementId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new CylinderStockMovementId(g)
            : throw new FormatException($"Invalid CylinderStockMovementId: '{s}'");
}
