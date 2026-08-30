using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPackageFeatureConfiguration : IEntityTypeConfiguration<SubscriptionPackageFeature>
{
    public void Configure(EntityTypeBuilder<SubscriptionPackageFeature> builder)
    {
        builder.ToTable("subscription_package_features", schema: "platform");

        builder.HasKey(x => new { x.PackageVersionId, x.FeatureId }).HasName("pk_subscription_package_features");

        builder.Property(x => x.PackageVersionId)
            .HasConversion(id => id.Value, value => new SubscriptionPackageVersionId(value));

        builder.Property(x => x.FeatureId)
            .HasConversion(id => id.Value, value => new FeatureDefinitionId(value));

        builder.Property(x => x.Enabled).IsRequired();
        builder.Property(x => x.LimitValue);

        // No explicit FK for either side — matches FeaturePermissionConfiguration's own composite-key
        // join-table convention (application-layer referential integrity only).
        builder.HasIndex(x => x.FeatureId).HasDatabaseName("ix_subscription_package_features_feature_id");
    }
}
