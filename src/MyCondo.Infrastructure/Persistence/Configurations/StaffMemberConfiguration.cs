using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staff_members", schema: "payroll");

        builder.HasKey(x => x.Id).HasName("pk_staff_members");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new StaffMemberId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.FullName })
            .HasDatabaseName("ix_staff_members_tenant_id_full_name");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
