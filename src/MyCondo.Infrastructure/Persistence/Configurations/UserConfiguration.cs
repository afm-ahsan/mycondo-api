using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", schema: "identity");

        builder.HasKey(x => x.Id).HasName("pk_users");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.EmailConfirmed);
        builder.Property(x => x.LastLoginAtUtc);
        builder.Property(x => x.AvatarAttachmentId);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.Property(x => x.CreatedAtUtc);
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.DeletedBy);

        // Tenant-scoped uniqueness — leading tenant_id per multi-tenancy convention.
        builder.HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique()
            .HasDatabaseName("ux_users_tenant_id_email");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);

        builder.Ignore(x => x.DomainEvents);
    }
}
