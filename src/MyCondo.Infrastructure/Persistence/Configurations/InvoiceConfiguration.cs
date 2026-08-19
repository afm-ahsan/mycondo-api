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
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.InvoiceDate).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.AmountPaid).HasPrecision(18, 2).IsRequired();
        // Defaulted so adding this column against a non-empty invoices table (an already-deployed
        // environment) doesn't fail — every pre-existing invoice genuinely has zero waived amount.
        builder.Property(x => x.WaivedAmount).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.LedgerPostingId)
            .HasConversion(id => id.Value, value => new LedgerPostingId(value))
            .IsRequired();

        // No DB-level default here (unlike WaivedAmount): LedgerAccountType.ResidentReceivable is the
        // enum's CLR default (0), so EF's default-value sentinel logic would treat that legitimate
        // value as "unset" and silently substitute the DB default instead of writing it — see the
        // migration's own comment. IncomeAccountType is never actually ResidentReceivable in practice
        // (every call site passes a real income role), so this is a latent footgun, not a live bug,
        // but it's not worth carrying. There are no pre-existing invoice rows to backfill against
        // today, so requiring every insert to supply it explicitly (failing loudly if a future
        // populated environment ever needs this migration without one) is the safer choice.
        builder.Property(x => x.IncomeAccountType).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(x => x.IssuedAtUtc).IsRequired();
        builder.Property(x => x.VoidedAtUtc);
        builder.Property(x => x.VoidedBy);
        builder.Property(x => x.VoidReason).HasMaxLength(500);

        builder.Property(x => x.VoidLedgerPostingId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new LedgerPostingId(value.Value) : (LedgerPostingId?)null);

        builder.Property(x => x.WaivedAtUtc);
        builder.Property(x => x.WaivedBy);
        builder.Property(x => x.WaiveReason).HasMaxLength(500);

        builder.Property(x => x.WaiveLedgerPostingId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new LedgerPostingId(value.Value) : (LedgerPostingId?)null);

        builder.Property(x => x.ResponsiblePartyType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ResponsibleResidentId);
        builder.Property(x => x.ResponsibleFlatOwnershipId);
        builder.Property(x => x.ResponsibleOccupancyRegistrationId);

        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.Ignore(x => x.Balance);

        builder.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("ux_invoices_tenant_id_invoice_number");

        // Authoritative duplicate-generation guard — see plan §6. Handler does a
        // check-before-insert against these same dimensions; this is the race-condition backstop.
        // Source is part of the key (Slice F) so a flat can have both a ServiceCharge invoice and a
        // Utility invoice for the same calendar period. Slice G narrows this to exclude
        // FacilityBooking: that source is a one-off charge, not a recurring-period one (two same-day
        // bookings for the same flat would otherwise collide on PeriodStart/PeriodEnd), and uniqueness
        // for booking invoices is instead guaranteed structurally by Booking's own single InvoiceId.
        // The Billing↔Finance integration template excludes Fine for the same reason — two fines for
        // the same flat can legitimately share a period.
        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.PeriodStart, x.PeriodEnd, x.Source })
            .IsUnique()
            .HasFilter("source <> 'FacilityBooking' AND source <> 'Fine'")
            .HasDatabaseName("ux_invoices_tenant_id_flat_id_period_source");

        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.Status })
            .HasDatabaseName("ix_invoices_tenant_id_building_id_status");

        // FIFO outstanding-invoice lookup: tenant+flat+status, ordered by due date at query time.
        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.Status })
            .HasDatabaseName("ix_invoices_tenant_id_flat_id_status");

        builder.Ignore(x => x.DomainEvents);
    }
}
