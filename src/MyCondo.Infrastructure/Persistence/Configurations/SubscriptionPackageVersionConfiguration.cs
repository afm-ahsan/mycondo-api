using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPackageVersionConfiguration : IEntityTypeConfiguration<SubscriptionPackageVersion>
{
    public void Configure(EntityTypeBuilder<SubscriptionPackageVersion> builder)
    {
        builder.ToTable("subscription_package_versions", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_subscription_package_versions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SubscriptionPackageVersionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.PackageId)
            .HasConversion(id => id.Value, value => new SubscriptionPackageId(value))
            .IsRequired();

        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveUntil);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
        builder.Property(x => x.QuarterlyPrice).HasPrecision(18, 2);
        builder.Property(x => x.SemiAnnualPrice).HasPrecision(18, 2);
        builder.Property(x => x.AnnualPrice).HasPrecision(18, 2);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);

        builder.HasIndex(x => new { x.PackageId, x.Version })
            .IsUnique()
            .HasDatabaseName("ux_subscription_package_versions_package_id_version");

        // Deterministic "current version" rule (ADR-033 §8): at most one Active version per package,
        // enforced here rather than by date-range overlap math — see SubscriptionPackageVersion's own
        // doc comment for the full rationale. Postgres partial unique index, not an EXCLUDE/btree_gist
        // constraint, since there is exactly one boolean condition ("is this row Active"), not a range
        // overlap to guard against.
        builder.HasIndex(x => x.PackageId)
            .IsUnique()
            .HasDatabaseName("ux_subscription_package_versions_package_id_active")
            .HasFilter("status = 'Active'");

        // No DB-level FK for PackageId — matches FeaturePermission/InvoiceLine's own established
        // convention for a same-layer parent pointer (application-layer referential integrity only).
        builder.Ignore(x => x.DomainEvents);
    }
}
