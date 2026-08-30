using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FeatureDefinitionConfiguration : IEntityTypeConfiguration<FeatureDefinition>
{
    public void Configure(EntityTypeBuilder<FeatureDefinition> builder)
    {
        builder.ToTable("feature_definitions", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_feature_definitions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FeatureDefinitionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Module).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.IsCore).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.EntitlementType).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.ParentFeatureId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FeatureDefinitionId?)null : new FeatureDefinitionId(value.Value));

        builder.HasIndex(x => x.Key).IsUnique().HasDatabaseName("ux_feature_definitions_key");
        builder.HasIndex(x => x.Module).HasDatabaseName("ix_feature_definitions_module");
        builder.HasIndex(x => x.ParentFeatureId).HasDatabaseName("ix_feature_definitions_parent_feature_id");

        // No DB-level self-referencing FK for ParentFeatureId — matches this codebase's established
        // convention for a nullable "soft" pointer within the same table (see Meter.ReplacesMeterId,
        // which is likewise a plain nullable-id column with no HasOne/WithMany relationship configured).
        // Parent/child integrity (no self-parent, no dangling parent key) is validated at
        // catalogue-authoring time by the seeder, per ADR-033 §9's own "package-authoring-time
        // validation, never inferred at resolution time" approach.
    }
}
