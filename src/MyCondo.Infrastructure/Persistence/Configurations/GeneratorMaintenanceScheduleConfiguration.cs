using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GeneratorMaintenanceScheduleConfiguration : IEntityTypeConfiguration<GeneratorMaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<GeneratorMaintenanceSchedule> builder)
    {
        builder.ToTable("generator_maintenance_schedules", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_generator_maintenance_schedules");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GeneratorMaintenanceScheduleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.GeneratorId)
            .HasConversion(id => id.Value, value => new GeneratorId(value))
            .IsRequired();
        builder.Property(x => x.NextDueDate);
        builder.Property(x => x.NextDueHourMeterReading).HasPrecision(12, 2);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.GeneratorId, x.IsActive })
            .HasDatabaseName("ix_generator_maintenance_schedules_tenant_id_generator_id_is_active");
    }
}
