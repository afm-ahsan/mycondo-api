using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ParcelConfiguration : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> builder)
    {
        builder.ToTable("parcels", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_parcels");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ParcelId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ParcelReference).HasMaxLength(80);
        builder.Property(x => x.CourierProvider).HasMaxLength(120);
        builder.Property(x => x.TrackingNumber).HasMaxLength(120);
        builder.Property(x => x.SenderName).HasMaxLength(200);

        builder.Property(x => x.RecipientFlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();

        builder.Property(x => x.RecipientResidentId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ResidentId(value.Value) : (ResidentId?)null);

        builder.Property(x => x.ParcelType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PackageCount).IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.ReceivedBy);
        builder.Property(x => x.StorageLocation).HasMaxLength(200);
        builder.Property(x => x.NotificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CollectedAtUtc);
        builder.Property(x => x.CollectedBy);
        builder.Property(x => x.CollectorName).HasMaxLength(200);
        builder.Property(x => x.CollectionAcknowledgement).HasMaxLength(200);
        builder.Property(x => x.DamageNote).HasMaxLength(500);
        builder.Property(x => x.CloseReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.ParcelReference })
            .IsUnique()
            .HasFilter("parcel_reference IS NOT NULL")
            .HasDatabaseName("ux_parcels_tenant_id_parcel_reference");

        builder.HasIndex(x => new { x.TenantId, x.RecipientFlatId })
            .HasDatabaseName("ix_parcels_tenant_id_recipient_flat_id");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_parcels_tenant_id_status");

        builder.Ignore(x => x.DomainEvents);
    }
}
