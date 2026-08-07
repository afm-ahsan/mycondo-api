using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.ToTable("facilities", schema: "amenities");

        builder.HasKey(x => x.Id).HasName("pk_facilities");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FacilityId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.FacilityType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Capacity).IsRequired();
        builder.Property(x => x.OperatingHoursStart);
        builder.Property(x => x.OperatingHoursEnd);
        builder.Property(x => x.RequiresApproval).IsRequired();
        builder.Property(x => x.BookingChargeAmount).HasPrecision(18, 2);
        builder.Property(x => x.DepositAmount).HasPrecision(18, 2);
        builder.Property(x => x.CancellationDeadlineHours).IsRequired();
        builder.Property(x => x.CancellationDeductionPercentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.GuestFeeAmount).HasPrecision(18, 2);
        builder.Property(x => x.MinimumAgeUnaccompanied);
        builder.Property(x => x.RequiresSafetyAcknowledgement).IsRequired();
        builder.Property(x => x.BlocksEntryIfAccountOverdue).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.FacilityType })
            .HasDatabaseName("ix_facilities_tenant_id_building_id_facility_type");

        builder.Ignore(x => x.DomainEvents);
    }
}
