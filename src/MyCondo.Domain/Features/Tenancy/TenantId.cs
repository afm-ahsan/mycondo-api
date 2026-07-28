namespace MyCondo.Domain.Features.Tenancy;

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static TenantId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new TenantId(g)
            : throw new FormatException($"Invalid TenantId: '{s}'");
}
