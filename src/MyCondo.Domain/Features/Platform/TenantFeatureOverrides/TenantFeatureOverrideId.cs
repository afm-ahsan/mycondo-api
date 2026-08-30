namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

public readonly record struct TenantFeatureOverrideId(Guid Value)
{
    public static TenantFeatureOverrideId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
