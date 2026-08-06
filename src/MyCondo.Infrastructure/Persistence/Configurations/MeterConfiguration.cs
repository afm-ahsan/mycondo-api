using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class MeterConfiguration : IEntityTypeConfiguration<Meter>
{
    public void Configure(EntityTypeBuilder<Meter> builder)
    {
        builder.ToTable("meters", schema: "utilities");

        builder.HasKey(x => x.Id).HasName("pk_meters");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new MeterId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.UtilityType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.MeterNumber).IsRequired().HasMaxLength(60);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.ReplacesMeterId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new MeterId(value.Value) : (MeterId?)null);

        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.UtilityType, x.MeterNumber })
            .IsUnique()
            .HasDatabaseName("ux_meters_tenant_id_utility_type_meter_number");

        builder.HasIndex(x => new { x.TenantId, x.BuildingId })
            .HasDatabaseName("ix_meters_tenant_id_building_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
