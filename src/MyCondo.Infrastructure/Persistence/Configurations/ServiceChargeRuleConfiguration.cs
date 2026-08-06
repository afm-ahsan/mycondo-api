using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ServiceChargeRuleConfiguration : IEntityTypeConfiguration<ServiceChargeRule>
{
    public void Configure(EntityTypeBuilder<ServiceChargeRule> builder)
    {
        builder.ToTable("service_charge_rules", schema: "billing");

        builder.HasKey(x => x.Id).HasName("pk_service_charge_rules");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceChargeRuleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CalculationMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.UnitTypeFilter).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Frequency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId })
            .HasDatabaseName("ix_service_charge_rules_tenant_id_building_id");

        // The authoritative overlap guard is a Postgres EXCLUDE constraint added via raw SQL in
        // Add_Billing_ServiceChargeRules_Invoices (EF Core has no fluent API for EXCLUDE/GiST) — see
        // ServiceChargeRule's doc comment and IServiceChargeRuleRepository.HasOverlappingRuleAsync
        // for the application-layer pre-check that gives a friendly error before hitting it.

        builder.Ignore(x => x.DomainEvents);
    }
}
