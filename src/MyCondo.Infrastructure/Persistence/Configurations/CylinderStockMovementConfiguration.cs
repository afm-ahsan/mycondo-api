using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class CylinderStockMovementConfiguration : IEntityTypeConfiguration<CylinderStockMovement>
{
    public void Configure(EntityTypeBuilder<CylinderStockMovement> builder)
    {
        builder.ToTable("cylinder_stock_movements", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_cylinder_stock_movements");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CylinderStockMovementId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CylinderType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.RecordedBy);
        builder.Property(x => x.CylinderPurchaseId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new CylinderPurchaseId(value.Value) : (CylinderPurchaseId?)null);

        builder.HasIndex(x => new { x.TenantId, x.CylinderType, x.OccurredAtUtc })
            .HasDatabaseName("ix_cylinder_stock_movements_tenant_id_cylinder_type_occurred_at_utc");
    }
}
