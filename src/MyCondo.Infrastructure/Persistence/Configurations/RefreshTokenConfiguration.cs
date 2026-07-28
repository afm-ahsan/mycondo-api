using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", schema: "identity");

        builder.HasKey(x => x.Id).HasName("pk_refresh_tokens");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new RefreshTokenId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByIp).IsRequired().HasMaxLength(64);
        builder.Property(x => x.RevokedAtUtc);
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);
        builder.Property(x => x.ReplacedByTokenId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                v => v.HasValue ? new RefreshTokenId(v.Value) : null);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_refresh_tokens_token_hash");

        builder.HasIndex(x => new { x.TenantId, x.UserId })
            .HasDatabaseName("ix_refresh_tokens_tenant_id_user_id");
    }
}
