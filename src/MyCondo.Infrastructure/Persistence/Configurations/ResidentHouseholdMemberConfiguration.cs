using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Residents.HouseholdMembers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ResidentHouseholdMemberConfiguration : IEntityTypeConfiguration<ResidentHouseholdMember>
{
    public void Configure(EntityTypeBuilder<ResidentHouseholdMember> builder)
    {
        builder.ToTable("household_members", schema: "residents");

        builder.HasKey(x => x.Id).HasName("pk_resident_household_members");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ResidentHouseholdMemberId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ResidentId).IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RelationshipType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Gender).IsRequired().HasMaxLength(20);
        builder.Property(x => x.DateOfBirth).IsRequired();
        builder.Property(x => x.NationalIdNumber).HasMaxLength(50);
        builder.Property(x => x.BirthCertificateNumber).HasMaxLength(50);
        builder.Property(x => x.BloodGroup).HasMaxLength(10);
        builder.Property(x => x.Religion).HasMaxLength(50);
        builder.Property(x => x.Nationality).HasMaxLength(50);
        builder.Property(x => x.Occupation).HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.PrimaryPhotoAttachmentId);

        builder.HasIndex(x => new { x.TenantId, x.ResidentId })
            .HasDatabaseName("ix_resident_household_members_tenant_id_resident_id");
    }
}
