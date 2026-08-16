using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("household_members", schema: "leasing");

        builder.HasKey(x => x.Id).HasName("pk_household_members");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new HouseholdMemberId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccupancyRegistrationId)
            .HasConversion(id => id.Value, value => new OccupancyRegistrationId(value))
            .IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RelationshipToPrimary).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DateOfBirth);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.NationalIdNumber).HasMaxLength(50);
        builder.Property(x => x.Gender).HasMaxLength(20);
        builder.Property(x => x.BirthCertificateNumber).HasMaxLength(50);
        builder.Property(x => x.BloodGroup).HasMaxLength(10);
        builder.Property(x => x.Religion).HasMaxLength(50);
        builder.Property(x => x.Nationality).HasMaxLength(50);
        builder.Property(x => x.Occupation).HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OccupancyRegistrationId })
            .HasDatabaseName("ix_household_members_tenant_id_occupancy_registration_id");
    }
}
