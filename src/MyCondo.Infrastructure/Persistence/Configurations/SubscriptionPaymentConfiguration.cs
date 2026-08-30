using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_subscription_payments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SubscriptionPaymentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SubscriptionInvoiceId)
            .HasConversion(id => id.Value, value => new SubscriptionInvoiceId(value))
            .IsRequired();

        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.PaymentDate).IsRequired();
        builder.Property(x => x.ReferenceNumber).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RecordedAtUtc).IsRequired();

        // Durable idempotency backstop (ADR-034 Task 14C) — the same payment reference cannot be
        // recorded twice for the same organization, mirroring ux_subscription_invoices_tenant_id_invoice_number's
        // role for Task 14B's invoice-number identity.
        builder.HasIndex(x => new { x.TenantId, x.ReferenceNumber })
            .IsUnique()
            .HasDatabaseName("ux_subscription_payments_tenant_id_reference_number");

        builder.HasIndex(x => x.SubscriptionInvoiceId)
            .HasDatabaseName("ix_subscription_payments_subscription_invoice_id");

        // No DB-level FK for TenantId/SubscriptionInvoiceId — matches SubscriptionInvoice's established
        // no-FK convention for cross-aggregate platform references.
        builder.Ignore(x => x.DomainEvents);
    }
}
