using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", schema: "identity");

        builder.HasKey(x => x.Id).HasName("pk_roles");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new RoleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.IsSystem).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_roles_tenant_id_name");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
