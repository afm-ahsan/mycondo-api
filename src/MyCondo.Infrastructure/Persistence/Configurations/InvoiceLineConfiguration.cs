using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines", schema: "billing");

        builder.HasKey(x => x.Id).HasName("pk_invoice_lines");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new InvoiceLineId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.InvoiceId)
            .HasConversion(id => id.Value, value => new InvoiceId(value))
            .IsRequired();

        builder.Property(x => x.ServiceChargeRuleId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ServiceChargeRuleId(value.Value) : (ServiceChargeRuleId?)null);

        builder.Property(x => x.RuleNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RuleCategorySnapshot).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CalculationMethodSnapshot).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RateSnapshot).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.AreaSqFtSnapshot).HasPrecision(10, 2);
        builder.Property(x => x.Quantity).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.LineAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.InvoiceId })
            .HasDatabaseName("ix_invoice_lines_tenant_id_invoice_id");

        // Transitively enforces "one line per (flat, rule, period)" together with the invoice-level
        // (tenant, flat, period) uniqueness — there can only be one invoice for a given (flat,
        // period), so uniqueness of (invoice, rule) here is uniqueness of (flat, rule, period) too.
        // ServiceChargeRuleId is nullable; Postgres unique indexes allow multiple NULLs, so future
        // non-rule (e.g. manual adjustment) lines won't collide.
        builder.HasIndex(x => new { x.TenantId, x.InvoiceId, x.ServiceChargeRuleId })
            .IsUnique()
            .HasDatabaseName("ux_invoice_lines_tenant_id_invoice_id_rule_id");
    }
}
