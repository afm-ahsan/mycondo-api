using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", schema: "billing");

        builder.HasKey(x => x.Id).HasName("pk_invoices");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new InvoiceId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(60);
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.InvoiceDate).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.AmountPaid).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.LedgerPostingId)
            .HasConversion(id => id.Value, value => new LedgerPostingId(value))
            .IsRequired();

        builder.Property(x => x.IssuedAtUtc).IsRequired();
        builder.Property(x => x.VoidedAtUtc);
        builder.Property(x => x.VoidedBy);
        builder.Property(x => x.VoidReason).HasMaxLength(500);

        builder.Property(x => x.VoidLedgerPostingId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new LedgerPostingId(value.Value) : (LedgerPostingId?)null);

        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.Ignore(x => x.Balance);

        builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("ux_invoices_tenant_id_invoice_number");

        // Authoritative duplicate-generation guard — see plan §6. Handler does a
        // check-before-insert against these same dimensions; this is the race-condition backstop.
        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.PeriodStart, x.PeriodEnd })
            .IsUnique()
            .HasDatabaseName("ux_invoices_tenant_id_flat_id_period");

        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.Status })
            .HasDatabaseName("ix_invoices_tenant_id_building_id_status");

        // FIFO outstanding-invoice lookup: tenant+flat+status, ordered by due date at query time.
        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.Status })
            .HasDatabaseName("ix_invoices_tenant_id_flat_id_status");

        builder.Ignore(x => x.DomainEvents);
    }
}
