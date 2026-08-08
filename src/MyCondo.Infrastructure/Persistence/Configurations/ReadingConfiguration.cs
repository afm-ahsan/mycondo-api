using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ReadingConfiguration : IEntityTypeConfiguration<Reading>
{
    public void Configure(EntityTypeBuilder<Reading> builder)
    {
        builder.ToTable("readings", schema: "utilities");

        builder.HasKey(x => x.Id).HasName("pk_readings");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ReadingId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MeterId)
            .HasConversion(id => id.Value, value => new MeterId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.UtilityType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.PreviousReading).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.PresentReading).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ConsumptionUnits).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ReadingDate).IsRequired();
        builder.Property(x => x.OverrideReason).HasMaxLength(500);
        builder.Property(x => x.IsAbnormalConsumption).IsRequired();
        builder.Property(x => x.AbnormalConsumptionReason).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReviewedAtUtc);
        builder.Property(x => x.ReviewedBy);
        builder.Property(x => x.FinalizedAtUtc);
        builder.Property(x => x.FinalizedBy);
        builder.Property(x => x.BilledAtUtc);
        builder.Property(x => x.BilledBy);

        builder.Property(x => x.InvoiceId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new InvoiceId(value.Value) : (InvoiceId?)null);

        builder.Property(x => x.CorrectsReadingId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ReadingId(value.Value) : (ReadingId?)null);

        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.MeterId, x.Status })
            .HasDatabaseName("ix_readings_tenant_id_meter_id_status");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_readings_tenant_id_flat_id");

        // Supports UX-5's tenant/building/utilityType-wide consumption and reading-status aggregate
        // reports — the prior indexes only covered the per-meter/per-flat lookup paths.
        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.UtilityType, x.Status })
            .HasDatabaseName("ix_readings_tenant_id_building_id_utility_type_status");

        // "One finalized reading per meter/period" — Corrected readings are excluded so a correction
        // can exist for the same (meter, period) as the reading it supersedes. Same partial-unique
        // pattern as AttendanceRecord's open-record index / MeterAssignment's open-assignment index.
        builder.HasIndex(x => new { x.TenantId, x.MeterId, x.PeriodStart, x.PeriodEnd })
            .IsUnique()
            .HasFilter("status <> 'Corrected'")
            .HasDatabaseName("ux_readings_tenant_id_meter_id_period_active");

        builder.Ignore(x => x.DomainEvents);
    }
}
