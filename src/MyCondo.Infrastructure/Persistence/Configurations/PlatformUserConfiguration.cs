using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.ToTable("platform_users", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_platform_users");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PlatformUserId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ux_platform_users_email");

        builder.Ignore(x => x.DomainEvents);
    }
}
