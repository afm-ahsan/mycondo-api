using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ParcelCustodyEventConfiguration : IEntityTypeConfiguration<ParcelCustodyEvent>
{
    public void Configure(EntityTypeBuilder<ParcelCustodyEvent> builder)
    {
        builder.ToTable("parcel_custody_events", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_parcel_custody_events");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ParcelCustodyEventId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ParcelId)
            .HasConversion(id => id.Value, value => new ParcelId(value))
            .IsRequired();
        builder.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.PerformedBy);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.ParcelId })
            .HasDatabaseName("ix_parcel_custody_events_tenant_id_parcel_id");
    }
}
