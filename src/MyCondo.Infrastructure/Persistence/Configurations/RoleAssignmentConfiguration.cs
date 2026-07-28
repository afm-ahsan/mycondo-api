using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("role_assignments", schema: "identity");

        builder.HasKey(x => x.Id).HasName("pk_role_assignments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new RoleAssignmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(x => x.RoleId)
            .HasConversion(id => id.Value, value => new RoleId(value));
        builder.Property(x => x.BuildingId);
        builder.Property(x => x.GrantedAtUtc).IsRequired();

        builder.Property(x => x.CreatedAtUtc);
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.RoleId, x.BuildingId })
            .IsUnique()
            .HasDatabaseName("ux_role_assignments_user_role_building");

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("ix_role_assignments_tenant_id_user_id");
    }
}
