namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

public readonly record struct FeatureDefinitionId(Guid Value)
{
    public static FeatureDefinitionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
