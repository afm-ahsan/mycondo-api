using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GuestProfileConfiguration : IEntityTypeConfiguration<GuestProfile>
{
    public void Configure(EntityTypeBuilder<GuestProfile> builder)
    {
        builder.ToTable("guest_profiles", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_guest_profiles");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GuestProfileId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.IdentityDocumentType).HasMaxLength(40);
        builder.Property(x => x.IdentityDocumentNumber).HasMaxLength(60);
        builder.Property(x => x.IsBlocked).IsRequired();
        builder.Property(x => x.BlockReason).HasMaxLength(400);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.Phone })
            .IsUnique()
            .HasDatabaseName("ux_guest_profiles_tenant_id_phone");

        builder.Ignore(x => x.DomainEvents);
    }
}
