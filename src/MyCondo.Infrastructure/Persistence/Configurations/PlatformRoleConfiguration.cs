using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        builder.ToTable("platform_roles", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_platform_roles");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PlatformRoleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.IsSystem).IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("ux_platform_roles_name");

        builder.Ignore(x => x.DomainEvents);
    }
}
