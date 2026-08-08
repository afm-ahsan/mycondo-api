using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", schema: "payments");

        builder.HasKey(x => x.Id).HasName("pk_payments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PaymentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(120);
        builder.Property(x => x.BusinessDate).IsRequired();
        builder.Property(x => x.ReceivedBy);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.LedgerPostingId)
            .HasConversion(id => id.Value, value => new LedgerPostingId(value))
            .IsRequired();

        builder.Property(x => x.ReversedAtUtc);
        builder.Property(x => x.ReversedBy);
        builder.Property(x => x.ReversalReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_payments_tenant_id_flat_id");

        // Supports both SearchAsync's status/date-range filters and UX-5's GetTotalCollectedAsync
        // aggregate — neither had a covering index before this.
        builder.HasIndex(x => new { x.TenantId, x.Status, x.BusinessDate })
            .HasDatabaseName("ix_payments_tenant_id_status_business_date");

        builder.HasIndex(x => x.LedgerPostingId)
            .IsUnique()
            .HasDatabaseName("ux_payments_ledger_posting_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
