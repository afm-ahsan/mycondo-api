using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.Permissions;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", schema: "identity");

        builder.HasKey(x => x.Id).HasName("pk_permissions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PermissionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.Module).IsRequired().HasMaxLength(40);
        builder.Property(x => x.IsBuildingScopable).IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("ux_permissions_name");
    }
}
