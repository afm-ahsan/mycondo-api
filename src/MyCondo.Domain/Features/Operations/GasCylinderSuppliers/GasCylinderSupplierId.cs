namespace MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

public readonly record struct GasCylinderSupplierId(Guid Value)
{
    public static GasCylinderSupplierId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GasCylinderSupplierId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GasCylinderSupplierId(g)
            : throw new FormatException($"Invalid GasCylinderSupplierId: '{s}'");
}
