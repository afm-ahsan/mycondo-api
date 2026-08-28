using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FeaturePermissionConfiguration : IEntityTypeConfiguration<FeaturePermission>
{
    public void Configure(EntityTypeBuilder<FeaturePermission> builder)
    {
        builder.ToTable("feature_permissions", schema: "platform");

        builder.HasKey(x => new { x.FeatureId, x.PermissionCode }).HasName("pk_feature_permissions");

        builder.Property(x => x.FeatureId)
            .HasConversion(id => id.Value, value => new FeatureDefinitionId(value));

        builder.Property(x => x.PermissionCode).IsRequired().HasMaxLength(100);

        // No explicit FK for FeatureId — matches RolePermissionConfiguration's own composite-key
        // join-table convention (no HasOne/WithMany relationship configured there either). PermissionCode
        // additionally has no natural relational id to reference at all (ADR-033 §5, mirrors
        // PermissionCatalogue's "plain seeded catalogue" convention). Both sides are application-layer
        // referential integrity, consistent with this schema's established pattern.
        builder.HasIndex(x => x.PermissionCode).HasDatabaseName("ix_feature_permissions_permission_code");
    }
}
