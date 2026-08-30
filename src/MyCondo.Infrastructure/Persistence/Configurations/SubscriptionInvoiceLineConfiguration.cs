using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionInvoiceLineConfiguration : IEntityTypeConfiguration<SubscriptionInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoiceLine> builder)
    {
        builder.ToTable("subscription_invoice_lines", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_subscription_invoice_lines");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SubscriptionInvoiceLineId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.SubscriptionInvoiceId)
            .HasConversion(id => id.Value, value => new SubscriptionInvoiceId(value))
            .IsRequired();

        builder.Property(x => x.PackageVersionId)
            .HasConversion(id => id.Value, value => new SubscriptionPackageVersionId(value))
            .IsRequired();

        builder.Property(x => x.PackageNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PackageVersionNumberSnapshot).IsRequired();
        builder.Property(x => x.BillingCycleSnapshot).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.BasePriceSnapshot).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DiscountSnapshot).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.LineAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.SubscriptionInvoiceId)
            .HasDatabaseName("ix_subscription_invoice_lines_subscription_invoice_id");
    }
}
