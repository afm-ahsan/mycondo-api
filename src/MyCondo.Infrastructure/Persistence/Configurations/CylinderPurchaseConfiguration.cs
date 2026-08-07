using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class CylinderPurchaseConfiguration : IEntityTypeConfiguration<CylinderPurchase>
{
    public void Configure(EntityTypeBuilder<CylinderPurchase> builder)
    {
        builder.ToTable("cylinder_purchases", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_cylinder_purchases");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CylinderPurchaseId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SupplierId)
            .HasConversion(id => id.Value, value => new GasCylinderSupplierId(value))
            .IsRequired();
        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PurchaseDate).IsRequired();
        builder.Property(x => x.CylinderType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.CylinderWeightKg).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.RatePerCylinder).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.DeliveryOrOtherCost).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ApprovedBy);
        builder.Property(x => x.ApprovedAtUtc);
        builder.Property(x => x.RejectedReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.Ignore(x => x.TotalKg);
        builder.Ignore(x => x.LineAmount);
        builder.Ignore(x => x.UnitPricePerKg);
        builder.Ignore(x => x.GrandTotal);

        builder.HasIndex(x => new { x.TenantId, x.SupplierId, x.PurchaseDate })
            .HasDatabaseName("ix_cylinder_purchases_tenant_id_supplier_id_purchase_date");

        builder.Ignore(x => x.DomainEvents);
    }
}
