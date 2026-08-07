using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GeneratorServiceRecordConfiguration : IEntityTypeConfiguration<GeneratorServiceRecord>
{
    public void Configure(EntityTypeBuilder<GeneratorServiceRecord> builder)
    {
        builder.ToTable("generator_service_records", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_generator_service_records");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GeneratorServiceRecordId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.GeneratorId)
            .HasConversion(id => id.Value, value => new GeneratorId(value))
            .IsRequired();
        builder.Property(x => x.PerformedAtUtc).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Cost).HasPrecision(12, 2);
        builder.Property(x => x.PerformedBy);

        builder.HasIndex(x => new { x.TenantId, x.GeneratorId, x.PerformedAtUtc })
            .HasDatabaseName("ix_generator_service_records_tenant_id_generator_id_performed_at_utc");
    }
}
