using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", schema: "amenities");

        builder.HasKey(x => x.Id).HasName("pk_bookings");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BookingId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FacilityId)
            .HasConversion(id => id.Value, value => new FacilityId(value))
            .IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(120);
        builder.Property(x => x.StartAtUtc).IsRequired();
        builder.Property(x => x.EndAtUtc).IsRequired();
        builder.Property(x => x.SetupBufferMinutes).IsRequired();
        builder.Property(x => x.CleanupBufferMinutes).IsRequired();
        builder.Property(x => x.ExpectedGuestCount).IsRequired();
        builder.Property(x => x.BookingChargeAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DepositAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CancellationDeadlineHours).IsRequired();
        builder.Property(x => x.CancellationDeductionPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.ApprovalRequired).IsRequired();
        builder.Property(x => x.PaymentRequired).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(x => x.InvoiceId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new InvoiceId(value.Value) : (InvoiceId?)null);

        builder.Property(x => x.DepositCollectionPostingId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new LedgerPostingId(value.Value) : (LedgerPostingId?)null);

        builder.Property(x => x.DepositSettlementPostingId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new LedgerPostingId(value.Value) : (LedgerPostingId?)null);

        builder.Property(x => x.DepositRefundedAmount).HasPrecision(18, 2);
        builder.Property(x => x.DepositDeductedAmount).HasPrecision(18, 2);
        builder.Property(x => x.TermsAcceptedAtUtc);
        builder.Property(x => x.ApprovedBy);
        builder.Property(x => x.ApprovedAtUtc);
        builder.Property(x => x.RejectedReason).HasMaxLength(500);
        builder.Property(x => x.CancelledReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy);
        builder.Property(x => x.CancelledAtUtc);
        builder.Property(x => x.CheckedInBy);
        builder.Property(x => x.CheckedInAtUtc);
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.InspectedBy);
        builder.Property(x => x.InspectedAtUtc);
        builder.Property(x => x.InspectionNotes).HasMaxLength(2000);
        builder.Property(x => x.DamageDeductionReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.Status })
            .HasDatabaseName("ix_bookings_tenant_id_flat_id_status");

        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.StartAtUtc })
            .HasDatabaseName("ix_bookings_tenant_id_facility_id_start_at_utc");

        builder.Ignore(x => x.DomainEvents);
    }
}
