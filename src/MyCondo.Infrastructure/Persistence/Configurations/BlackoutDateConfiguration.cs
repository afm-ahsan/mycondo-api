using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class BlackoutDateConfiguration : IEntityTypeConfiguration<BlackoutDate>
{
    public void Configure(EntityTypeBuilder<BlackoutDate> builder)
    {
        builder.ToTable("blackout_dates", schema: "amenities");

        builder.HasKey(x => x.Id).HasName("pk_blackout_dates");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BlackoutDateId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FacilityId)
            .HasConversion(id => id.Value, value => new FacilityId(value))
            .IsRequired();
        builder.Property(x => x.DateFrom).IsRequired();
        builder.Property(x => x.DateTo).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.IsActive })
            .HasDatabaseName("ix_blackout_dates_tenant_id_facility_id_is_active");
    }
}
