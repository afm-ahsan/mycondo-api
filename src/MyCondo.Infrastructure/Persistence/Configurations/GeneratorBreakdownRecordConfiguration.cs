using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GeneratorBreakdownRecordConfiguration : IEntityTypeConfiguration<GeneratorBreakdownRecord>
{
    public void Configure(EntityTypeBuilder<GeneratorBreakdownRecord> builder)
    {
        builder.ToTable("generator_breakdown_records", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_generator_breakdown_records");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GeneratorBreakdownRecordId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.GeneratorId)
            .HasConversion(id => id.Value, value => new GeneratorId(value))
            .IsRequired();
        builder.Property(x => x.ReportedAtUtc).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.DowntimeStartUtc).IsRequired();
        builder.Property(x => x.DowntimeEndUtc);
        builder.Property(x => x.Resolution).HasMaxLength(1000);
        builder.Property(x => x.Cost).HasPrecision(12, 2);

        builder.HasIndex(x => new { x.TenantId, x.GeneratorId, x.ReportedAtUtc })
            .HasDatabaseName("ix_generator_breakdown_records_tenant_id_generator_id_reported_at_utc");
    }
}
