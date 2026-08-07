using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class OccupancyRegistrationConfiguration : IEntityTypeConfiguration<OccupancyRegistration>
{
    public void Configure(EntityTypeBuilder<OccupancyRegistration> builder)
    {
        builder.ToTable("occupancy_registrations", schema: "leasing");

        builder.HasKey(x => x.Id).HasName("pk_occupancy_registrations");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.PrimaryResidentId)
            .HasConversion(id => id.Value, value => new ResidentId(value))
            .IsRequired();
        builder.Property(x => x.OccupancyType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PrimaryFullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PrimaryPhone).HasMaxLength(30);
        builder.Property(x => x.PrimaryEmail).HasMaxLength(200);
        builder.Property(x => x.PrimaryNationalIdNumber).HasMaxLength(50);
        builder.Property(x => x.PrimaryDateOfBirth);
        builder.Property(x => x.PrimaryPermanentAddress).HasMaxLength(500);
        builder.Property(x => x.EmergencyContactName).HasMaxLength(200);
        builder.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
        builder.Property(x => x.PrimaryPhotoAttachmentId);
        builder.Property(x => x.MoveInExpectedDate);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(25).IsRequired();
        builder.Property(x => x.SubmittedAtUtc);
        builder.Property(x => x.SubmittedBy);
        builder.Property(x => x.OwnerReviewedAtUtc);
        builder.Property(x => x.OwnerReviewedBy);
        builder.Property(x => x.ManagementVerifiedAtUtc);
        builder.Property(x => x.ManagementVerifiedBy);
        builder.Property(x => x.ActivatedAtUtc);
        builder.Property(x => x.MovedOutAtUtc);
        builder.Property(x => x.MoveOutReason).HasMaxLength(500);
        builder.Property(x => x.CorrectionsRequestedReason).HasMaxLength(500);
        builder.Property(x => x.RejectedReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.Status })
            .HasDatabaseName("ix_occupancy_registrations_tenant_id_flat_id_status");

        builder.Ignore(x => x.DomainEvents);
    }
}
