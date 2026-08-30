using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
    {
        builder.ToTable("subscription_invoices", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_subscription_invoices");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SubscriptionInvoiceId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OrganizationSubscriptionId)
            .HasConversion(id => id.Value, value => new OrganizationSubscriptionId(value))
            .IsRequired();

        builder.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(60);
        builder.Property(x => x.BillingPeriodStart).IsRequired();
        builder.Property(x => x.BillingPeriodEnd).IsRequired();
        builder.Property(x => x.IssueDate).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);

        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.OutstandingAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.IssuedAtUtc).IsRequired();
        builder.Property(x => x.PaidAtUtc);
        builder.Property(x => x.VoidedAtUtc);
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.CanceledAtUtc);
        builder.Property(x => x.CancelReason).HasMaxLength(500);

        // Invoice-number identity, scoped per organization (mirrors ux_invoices_tenant_id_invoice_number).
        builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("ux_subscription_invoices_tenant_id_invoice_number");

        // Smallest database-backed duplicate-generation guard this foundation needs: at most one
        // invoice per (subscription, billing period), regardless of lifecycle status — mirrors
        // ux_invoices_tenant_id_flat_id_period_source's role for tenant Billing. The invoice-generation
        // command (a later Task 14 slice) relies on this as its idempotency backstop rather than
        // reimplementing the check purely in application code.
        builder.HasIndex(x => new { x.OrganizationSubscriptionId, x.BillingPeriodStart, x.BillingPeriodEnd })
            .IsUnique()
            .HasDatabaseName("ux_subscription_invoices_subscription_id_period");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_subscription_invoices_tenant_id_status");

        // No DB-level FK for TenantId/OrganizationSubscriptionId — matches OrganizationSubscription's
        // established no-FK convention for cross-schema/cross-aggregate platform references.
        builder.Ignore(x => x.DomainEvents);
    }
}
