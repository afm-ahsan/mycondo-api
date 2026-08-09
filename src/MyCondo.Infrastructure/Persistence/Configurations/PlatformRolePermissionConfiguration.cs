using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PlatformRolePermissionConfiguration : IEntityTypeConfiguration<PlatformRolePermission>
{
    public void Configure(EntityTypeBuilder<PlatformRolePermission> builder)
    {
        builder.ToTable("platform_role_permissions", schema: "platform");

        builder.HasKey(x => new { x.PlatformRoleId, x.PermissionId })
            .HasName("pk_platform_role_permissions");

        builder.Property(x => x.PlatformRoleId)
            .HasConversion(id => id.Value, value => new PlatformRoleId(value));
        builder.Property(x => x.PermissionId)
            .HasConversion(id => id.Value, value => new PermissionId(value));

        builder.Property(x => x.GrantedAtUtc).IsRequired();
        builder.Property(x => x.GrantedBy);

        // No EF-level FK relationship into identity.permissions is declared here — same convention as
        // the existing RolePermissionConfiguration, which likewise leaves RoleId/PermissionId as plain
        // converted scalars with no navigation property. Referential validity is enforced at the
        // application layer (handlers/seeders only ever link a Permission.Id they've just looked up).
        builder.HasIndex(x => x.PermissionId).HasDatabaseName("ix_platform_role_permissions_permission_id");
    }
}
