using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PlatformUserRoleAssignmentConfiguration : IEntityTypeConfiguration<PlatformUserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformUserRoleAssignment> builder)
    {
        builder.ToTable("platform_user_role_assignments", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_platform_user_role_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PlatformUserRoleAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.PlatformUserId)
            .HasConversion(id => id.Value, value => new PlatformUserId(value));
        builder.Property(x => x.PlatformRoleId)
            .HasConversion(id => id.Value, value => new PlatformRoleId(value));

        builder.Property(x => x.GrantedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.PlatformUserId, x.PlatformRoleId })
            .IsUnique()
            .HasDatabaseName("ux_platform_user_role_assignments_user_id_role_id");
    }
}
