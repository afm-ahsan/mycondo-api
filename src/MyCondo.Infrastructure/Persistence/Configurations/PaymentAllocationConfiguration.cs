using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocations", schema: "payments");

        builder.HasKey(x => x.Id).HasName("pk_payment_allocations");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PaymentAllocationId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PaymentId)
            .HasConversion(id => id.Value, value => new PaymentId(value))
            .IsRequired();
        builder.Property(x => x.InvoiceId)
            .HasConversion(id => id.Value, value => new InvoiceId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.AllocatedAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.AllocatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.InvoiceId })
            .HasDatabaseName("ix_payment_allocations_tenant_id_invoice_id");

        builder.HasIndex(x => new { x.TenantId, x.PaymentId })
            .HasDatabaseName("ix_payment_allocations_tenant_id_payment_id");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_payment_allocations_tenant_id_flat_id");
    }
}
