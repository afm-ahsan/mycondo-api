using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions", schema: "identity");

        builder.HasKey(x => new { x.RoleId, x.PermissionId }).HasName("pk_role_permissions");

        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => new RoleId(value));
        builder.Property(x => x.PermissionId)
            .HasConversion(id => id.Value, value => new PermissionId(value));

        builder.Property(x => x.GrantedAtUtc).IsRequired();
        builder.Property(x => x.GrantedBy);

        builder.HasIndex(x => x.PermissionId).HasDatabaseName("ix_role_permissions_permission_id");
    }
}
