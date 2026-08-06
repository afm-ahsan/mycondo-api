using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class RatePlanConfiguration : IEntityTypeConfiguration<RatePlan>
{
    public void Configure(EntityTypeBuilder<RatePlan> builder)
    {
        builder.ToTable("rate_plans", schema: "utilities");

        builder.HasKey(x => x.Id).HasName("pk_rate_plans");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new RatePlanId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.UtilityType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Structure).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.FixedAmount).HasPrecision(18, 2);
        builder.Property(x => x.FixedServiceCharge).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TaxPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.UtilityType })
            .HasDatabaseName("ix_rate_plans_tenant_id_building_id_utility_type");

        // Authoritative overlap guard — same EXCLUDE/GiST pattern as ServiceChargeRule (Slice E),
        // added via raw SQL in the migration (no EF fluent API for EXCLUDE constraints).

        builder.Ignore(x => x.DomainEvents);
    }
}
