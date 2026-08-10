namespace MyCondo.Domain.Features.Tenancy;

public readonly record struct TenantModuleId(Guid Value)
{
    public static TenantModuleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
