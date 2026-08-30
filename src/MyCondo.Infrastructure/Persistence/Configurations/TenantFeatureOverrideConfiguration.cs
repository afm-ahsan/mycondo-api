using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class TenantFeatureOverrideConfiguration : IEntityTypeConfiguration<TenantFeatureOverride>
{
    public void Configure(EntityTypeBuilder<TenantFeatureOverride> builder)
    {
        builder.ToTable("tenant_feature_overrides", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_tenant_feature_overrides");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TenantFeatureOverrideId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FeatureId)
            .HasConversion(id => id.Value, value => new FeatureDefinitionId(value))
            .IsRequired();
        builder.Property(x => x.Enabled).IsRequired();

        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveUntil);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.FeatureId })
            .HasDatabaseName("ix_tenant_feature_overrides_tenant_id_feature_id");

        // The authoritative overlap guard is a Postgres EXCLUDE constraint added via raw SQL in the
        // owning migration (EF Core has no fluent-API representation for EXCLUDE/GiST) — same pattern
        // as ServiceChargeRule/RatePlan/Booking. See ITenantFeatureOverrideRepository
        // .HasOverlappingOverrideAsync for the application-layer pre-check that gives a friendly error
        // before that constraint fires.

        // No DB-level FK for TenantId/FeatureId — matches TenantModule/FeaturePermission's established
        // no-FK convention for cross-schema/cross-aggregate platform references.
    }
}
