using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSubscriptionConfiguration : IEntityTypeConfiguration<OrganizationSubscription>
{
    public void Configure(EntityTypeBuilder<OrganizationSubscription> builder)
    {
        builder.ToTable("organization_subscriptions", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_organization_subscriptions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OrganizationSubscriptionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PackageVersionId)
            .HasConversion(id => id.Value, value => new SubscriptionPackageVersionId(value))
            .IsRequired();

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate);
        builder.Property(x => x.NextBillingDate);

        builder.Property(x => x.BasePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Discount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.EffectivePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);

        builder.Property(x => x.ActivatedAt).IsRequired();
        builder.Property(x => x.RestrictedAt);
        builder.Property(x => x.ExpiredAt);
        builder.Property(x => x.CanceledAt);

        builder.Property(x => x.AutoRenew).IsRequired();

        // Deterministic "one current subscription per tenant" rule (ADR-033 §10, Task 03 §18):
        // Active/PastDue/Restricted are the non-terminal/"current" states; Expired/Canceled are
        // historical and may accumulate freely per tenant. Postgres partial unique index, mirroring
        // SubscriptionPackageVersion's own "one Active version per package" technique. This is also
        // the only TenantId index this table needs at MVP-1.1 — EF Core keys HasIndex(...) calls by
        // their property list, so a second, plain HasIndex(x => x.TenantId) would not add a separate
        // index alongside this one, only reconfigure it.
        builder.HasIndex(x => x.TenantId)
            .IsUnique()
            .HasDatabaseName("ux_organization_subscriptions_tenant_id_current")
            .HasFilter("status IN ('Active', 'PastDue', 'Restricted')");

        // No DB-level FK for TenantId/PackageVersionId — matches TenantModule/SubscriptionPackageVersion's
        // established no-FK convention for cross-schema/cross-aggregate platform references.
        builder.Ignore(x => x.DomainEvents);
    }
}
