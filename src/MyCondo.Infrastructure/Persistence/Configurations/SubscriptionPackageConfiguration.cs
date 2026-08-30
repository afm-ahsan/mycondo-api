using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPackageConfiguration : IEntityTypeConfiguration<SubscriptionPackage>
{
    public void Configure(EntityTypeBuilder<SubscriptionPackage> builder)
    {
        builder.ToTable("subscription_packages", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_subscription_packages");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SubscriptionPackageId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.CurrentVersionId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (SubscriptionPackageVersionId?)null : new SubscriptionPackageVersionId(value.Value));

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_subscription_packages_code");

        // No DB-level FK for CurrentVersionId — application-layer referential integrity, matching this
        // codebase's established no-FK convention for platform-schema pointer columns (see
        // FeatureDefinitionConfiguration.ParentFeatureId).
        builder.Ignore(x => x.DomainEvents);
    }
}
