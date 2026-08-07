using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GeneratorFuelReceiptConfiguration : IEntityTypeConfiguration<GeneratorFuelReceipt>
{
    public void Configure(EntityTypeBuilder<GeneratorFuelReceipt> builder)
    {
        builder.ToTable("generator_fuel_receipts", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_generator_fuel_receipts");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GeneratorFuelReceiptId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.GeneratorId)
            .HasConversion(id => id.Value, value => new GeneratorId(value))
            .IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Cost).HasPrecision(12, 2);
        builder.Property(x => x.Supplier).HasMaxLength(200);
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.GeneratorId, x.ReceivedAtUtc })
            .HasDatabaseName("ix_generator_fuel_receipts_tenant_id_generator_id_received_at_utc");
    }
}
